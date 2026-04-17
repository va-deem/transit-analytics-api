using Microsoft.Extensions.Options;
using TransitAnalyticsAPI.Configuration;
using TransitAnalyticsAPI.Models.Entities;
using TransitAnalyticsAPI.Services;
using TransitAnalyticsAPI.Tests.TestHelpers;
using Xunit;

namespace TransitAnalyticsAPI.Tests.Services;

public class StopDeparturesQueryServiceTests
{
    [Fact]
    public async Task GetUpcomingDeparturesAsync_ReturnsFutureDeparturesForActiveServiceDay()
    {
        await using var dbContext = TestDbContextFactory.CreateSqliteDbContext();
        dbContext.GtfsImportRuns.Add(new GtfsImportRun
        {
            Id = 1,
            SourceVersion = "test",
            StartedAtUtc = DateTime.UtcNow,
            Status = GtfsImportStatus.Completed,
            IsActive = true
        });
        dbContext.GtfsCalendars.Add(new GtfsCalendar
        {
            ImportRunId = 1,
            ServiceId = "weekday",
            Thursday = true,
            StartDate = new DateOnly(2026, 4, 1),
            EndDate = new DateOnly(2026, 4, 30)
        });
        dbContext.GtfsRoutes.Add(new GtfsRoute
        {
            ImportRunId = 1,
            RouteId = "route-1",
            RouteShortName = "NX1",
            RouteLongName = "Northern Express",
            RouteColor = "0055AA"
        });
        dbContext.GtfsTrips.Add(new GtfsTrip
        {
            ImportRunId = 1,
            TripId = "trip-1",
            RouteId = "route-1",
            ServiceId = "weekday",
            TripHeadsign = "Britomart"
        });
        dbContext.GtfsStopTimes.AddRange(
            new GtfsStopTime
            {
                ImportRunId = 1,
                TripId = "trip-1",
                StopId = "stop-1",
                ArrivalTimeSeconds = (14 * 3600) + (35 * 60),
                DepartureTimeSeconds = (14 * 3600) + (35 * 60),
                StopSequence = 10
            },
            new GtfsStopTime
            {
                ImportRunId = 1,
                TripId = "trip-1",
                StopId = "stop-1",
                ArrivalTimeSeconds = 12 * 3600,
                DepartureTimeSeconds = 12 * 3600,
                StopSequence = 9
            });
        dbContext.VehiclePositions.Add(new VehiclePosition
        {
            VehicleId = "bus-123",
            TripId = "trip-1",
            RouteId = "route-1",
            Latitude = -36.8,
            Longitude = 174.7,
            RecordedAtUtc = new DateTime(2026, 4, 16, 0, 58, 0, DateTimeKind.Utc),
            IngestedAtUtc = new DateTime(2026, 4, 16, 0, 58, 0, DateTimeKind.Utc)
        });
        await dbContext.SaveChangesAsync();

        var service = new StopDeparturesQueryService(
            dbContext,
            new FakeActiveImportRunResolver(1),
            new FakeTimeProvider(new DateTimeOffset(2026, 4, 16, 1, 0, 0, TimeSpan.Zero)),
            Options.Create(new VehicleOptions
            {
                LatestPositionMaxAgeMinutes = 5
            }));

        var departures = await service.GetUpcomingDeparturesAsync("stop-1", CancellationToken.None);

        var departure = Assert.Single(departures);
        Assert.Equal("NX1", departure.RouteShortName);
        Assert.Equal("Northern Express", departure.RouteLongName);
        Assert.Equal("Britomart", departure.TripHeadsign);
        Assert.Equal(new DateTime(2026, 4, 16, 2, 35, 0, DateTimeKind.Utc), departure.ScheduledArrival);
        Assert.Null(departure.EstimatedArrival);
        Assert.Equal("bus-123", departure.VehicleId);
        Assert.Equal("#0055AA", departure.RouteColor);
    }

    [Fact]
    public async Task GetUpcomingDeparturesAsync_HonorsCalendarDateExceptions()
    {
        await using var dbContext = TestDbContextFactory.CreateSqliteDbContext();
        dbContext.GtfsImportRuns.Add(new GtfsImportRun
        {
            Id = 1,
            SourceVersion = "test",
            StartedAtUtc = DateTime.UtcNow,
            Status = GtfsImportStatus.Completed,
            IsActive = true
        });
        dbContext.GtfsCalendars.AddRange(
            new GtfsCalendar
            {
                ImportRunId = 1,
                ServiceId = "weekday",
                Thursday = true,
                StartDate = new DateOnly(2026, 4, 1),
                EndDate = new DateOnly(2026, 4, 30)
            },
            new GtfsCalendar
            {
                ImportRunId = 1,
                ServiceId = "holiday",
                Thursday = false,
                StartDate = new DateOnly(2026, 4, 1),
                EndDate = new DateOnly(2026, 4, 30)
            });
        dbContext.GtfsCalendarDates.AddRange(
            new GtfsCalendarDate
            {
                ImportRunId = 1,
                ServiceId = "weekday",
                Date = new DateOnly(2026, 4, 16),
                ExceptionType = 2
            },
            new GtfsCalendarDate
            {
                ImportRunId = 1,
                ServiceId = "holiday",
                Date = new DateOnly(2026, 4, 16),
                ExceptionType = 1
            });
        dbContext.GtfsRoutes.Add(new GtfsRoute
        {
            ImportRunId = 1,
            RouteId = "route-1",
            RouteShortName = "NX1"
        });
        dbContext.GtfsTrips.AddRange(
            new GtfsTrip
            {
                ImportRunId = 1,
                TripId = "removed-trip",
                RouteId = "route-1",
                ServiceId = "weekday",
                TripHeadsign = "Removed"
            },
            new GtfsTrip
            {
                ImportRunId = 1,
                TripId = "added-trip",
                RouteId = "route-1",
                ServiceId = "holiday",
                TripHeadsign = "Added"
            });
        dbContext.GtfsStopTimes.AddRange(
            new GtfsStopTime
            {
                ImportRunId = 1,
                TripId = "removed-trip",
                StopId = "stop-1",
                ArrivalTimeSeconds = 15 * 3600,
                DepartureTimeSeconds = 15 * 3600,
                StopSequence = 1
            },
            new GtfsStopTime
            {
                ImportRunId = 1,
                TripId = "added-trip",
                StopId = "stop-1",
                ArrivalTimeSeconds = 16 * 3600,
                DepartureTimeSeconds = 16 * 3600,
                StopSequence = 1
            });
        await dbContext.SaveChangesAsync();

        var service = new StopDeparturesQueryService(
            dbContext,
            new FakeActiveImportRunResolver(1),
            new FakeTimeProvider(new DateTimeOffset(2026, 4, 16, 1, 0, 0, TimeSpan.Zero)),
            Options.Create(new VehicleOptions
            {
                LatestPositionMaxAgeMinutes = 5
            }));

        var departures = await service.GetUpcomingDeparturesAsync("stop-1", CancellationToken.None);

        var departure = Assert.Single(departures);
        Assert.Equal("Added", departure.TripHeadsign);
    }
}
