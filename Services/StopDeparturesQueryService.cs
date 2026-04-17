using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TransitAnalyticsAPI.Configuration;
using TransitAnalyticsAPI.Models.Dto;
using TransitAnalyticsAPI.Persistence;

namespace TransitAnalyticsAPI.Services;

public class StopDeparturesQueryService : IStopDeparturesQueryService
{
    private const int DefaultDepartureLimit = 25;
    private static readonly TimeZoneInfo AucklandTimeZone = CreateAucklandTimeZone();

    private readonly AppDbContext _appDbContext;
    private readonly IActiveImportRunResolver _activeImportRunResolver;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _latestPositionMaxAge;

    public StopDeparturesQueryService(
        AppDbContext appDbContext,
        IActiveImportRunResolver activeImportRunResolver,
        TimeProvider timeProvider,
        IOptions<VehicleOptions> vehicleOptions)
    {
        _appDbContext = appDbContext;
        _activeImportRunResolver = activeImportRunResolver;
        _timeProvider = timeProvider;
        _latestPositionMaxAge = TimeSpan.FromMinutes(Math.Max(1, vehicleOptions.Value.LatestPositionMaxAgeMinutes));
    }

    public async Task<List<StopDepartureDto>> GetUpcomingDeparturesAsync(
        string stopId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stopId))
        {
            return [];
        }

        long? activeImportRunId = await _activeImportRunResolver.GetActiveImportRunIdAsync(cancellationToken);
        if (!activeImportRunId.HasValue)
        {
            return [];
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, AucklandTimeZone);
        var serviceDate = DateOnly.FromDateTime(nowLocal);
        int secondsSinceServiceDayStart = (int)(nowLocal - serviceDate.ToDateTime(TimeOnly.MinValue)).TotalSeconds;
        var activeServiceIds = await GetActiveServiceIdsAsync(activeImportRunId.Value, serviceDate, cancellationToken);

        if (activeServiceIds.Count == 0)
        {
            return [];
        }

        var departures = await (
            from stopTime in _appDbContext.GtfsStopTimes.AsNoTracking()
            join trip in _appDbContext.GtfsTrips.AsNoTracking()
                on new { stopTime.ImportRunId, stopTime.TripId } equals new { trip.ImportRunId, trip.TripId }
            join route in _appDbContext.GtfsRoutes.AsNoTracking()
                on new { trip.ImportRunId, trip.RouteId } equals new { route.ImportRunId, route.RouteId }
            where stopTime.ImportRunId == activeImportRunId.Value
                && stopTime.StopId == stopId
                && trip.ServiceId != null
                && activeServiceIds.Contains(trip.ServiceId)
                && (stopTime.ArrivalTimeSeconds ?? stopTime.DepartureTimeSeconds) != null
                && (stopTime.ArrivalTimeSeconds ?? stopTime.DepartureTimeSeconds) >= secondsSinceServiceDayStart
                && _appDbContext.GtfsStopTimes.Any(nextStopTime =>
                    nextStopTime.ImportRunId == stopTime.ImportRunId &&
                    nextStopTime.TripId == stopTime.TripId &&
                    nextStopTime.StopSequence > stopTime.StopSequence)
            orderby stopTime.ArrivalTimeSeconds ?? stopTime.DepartureTimeSeconds, route.RouteShortName, trip.TripId
            select new
            {
                route.RouteShortName,
                route.RouteLongName,
                route.RouteColor,
                TripHeadsign = stopTime.StopHeadsign ?? trip.TripHeadsign,
                trip.TripId,
                ScheduledArrivalSeconds = stopTime.ArrivalTimeSeconds ?? stopTime.DepartureTimeSeconds
            })
            .Take(DefaultDepartureLimit)
            .ToListAsync(cancellationToken);

        if (departures.Count == 0)
        {
            return [];
        }

        var tripIds = departures
            .Select(departure => departure.TripId)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var cutoffUtc = nowUtc - _latestPositionMaxAge;
        var recentVehiclePositions = await _appDbContext.VehiclePositions
            .AsNoTracking()
            .Where(vehiclePosition =>
                vehiclePosition.RecordedAtUtc >= cutoffUtc &&
                vehiclePosition.TripId != null &&
                tripIds.Contains(vehiclePosition.TripId))
            .Select(vehiclePosition => new
            {
                TripId = vehiclePosition.TripId!,
                vehiclePosition.VehicleId,
                vehiclePosition.RecordedAtUtc,
                vehiclePosition.Id
            })
            .ToListAsync(cancellationToken);

        var vehicleIdsByTripId = recentVehiclePositions
            .GroupBy(vehiclePosition => vehiclePosition.TripId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(vehiclePosition => vehiclePosition.RecordedAtUtc)
                    .ThenByDescending(vehiclePosition => vehiclePosition.Id)
                    .Select(vehiclePosition => vehiclePosition.VehicleId)
                    .First(),
                StringComparer.Ordinal);

        return departures
            .Select(departure => new StopDepartureDto
            {
                RouteShortName = departure.RouteShortName ?? string.Empty,
                RouteLongName = departure.RouteLongName,
                TripHeadsign = departure.TripHeadsign,
                ScheduledArrival = ConvertServiceTimeToUtc(serviceDate, departure.ScheduledArrivalSeconds!.Value),
                EstimatedArrival = null,
                VehicleId = vehicleIdsByTripId.GetValueOrDefault(departure.TripId),
                RouteColor = FormatRouteColor(departure.RouteColor)
            })
            .ToList();
    }

    private async Task<HashSet<string>> GetActiveServiceIdsAsync(
        long importRunId,
        DateOnly serviceDate,
        CancellationToken cancellationToken)
    {
        var calendars = await _appDbContext.GtfsCalendars
            .AsNoTracking()
            .Where(calendar =>
                calendar.ImportRunId == importRunId &&
                calendar.StartDate <= serviceDate &&
                calendar.EndDate >= serviceDate)
            .Select(calendar => new CalendarDayAvailability
            {
                ServiceId = calendar.ServiceId,
                Monday = calendar.Monday,
                Tuesday = calendar.Tuesday,
                Wednesday = calendar.Wednesday,
                Thursday = calendar.Thursday,
                Friday = calendar.Friday,
                Saturday = calendar.Saturday,
                Sunday = calendar.Sunday
            })
            .ToListAsync(cancellationToken);

        var activeServiceIds = calendars
            .Where(calendar => IsActiveOnDay(calendar, serviceDate.DayOfWeek))
            .Select(calendar => calendar.ServiceId)
            .ToHashSet(StringComparer.Ordinal);

        var exceptions = await _appDbContext.GtfsCalendarDates
            .AsNoTracking()
            .Where(calendarDate => calendarDate.ImportRunId == importRunId && calendarDate.Date == serviceDate)
            .Select(calendarDate => new
            {
                calendarDate.ServiceId,
                calendarDate.ExceptionType
            })
            .ToListAsync(cancellationToken);

        foreach (var exception in exceptions)
        {
            switch (exception.ExceptionType)
            {
                case 1:
                    activeServiceIds.Add(exception.ServiceId);
                    break;
                case 2:
                    activeServiceIds.Remove(exception.ServiceId);
                    break;
            }
        }

        return activeServiceIds;
    }

    private static bool IsActiveOnDay(CalendarDayAvailability calendar, DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => calendar.Monday,
            DayOfWeek.Tuesday => calendar.Tuesday,
            DayOfWeek.Wednesday => calendar.Wednesday,
            DayOfWeek.Thursday => calendar.Thursday,
            DayOfWeek.Friday => calendar.Friday,
            DayOfWeek.Saturday => calendar.Saturday,
            DayOfWeek.Sunday => calendar.Sunday,
            _ => false
        };
    }

    private static DateTime ConvertServiceTimeToUtc(DateOnly serviceDate, int secondsFromMidnight)
    {
        var localDateTime = serviceDate.ToDateTime(TimeOnly.MinValue).AddSeconds(secondsFromMidnight);
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified), AucklandTimeZone);
    }

    private static string? FormatRouteColor(string? routeColor)
    {
        if (string.IsNullOrWhiteSpace(routeColor))
        {
            return null;
        }

        return routeColor.StartsWith('#') ? routeColor : $"#{routeColor}";
    }

    private static TimeZoneInfo CreateAucklandTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Pacific/Auckland");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("New Zealand Standard Time");
        }
    }

    private sealed class CalendarDayAvailability
    {
        public string ServiceId { get; set; } = string.Empty;

        public bool Monday { get; set; }

        public bool Tuesday { get; set; }

        public bool Wednesday { get; set; }

        public bool Thursday { get; set; }

        public bool Friday { get; set; }

        public bool Saturday { get; set; }

        public bool Sunday { get; set; }
    }
}
