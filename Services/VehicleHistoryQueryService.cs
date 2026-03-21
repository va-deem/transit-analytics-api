using Microsoft.EntityFrameworkCore;
using TransitAnalyticsAPI.Models.Dto;
using TransitAnalyticsAPI.Models.Entities;
using TransitAnalyticsAPI.Persistence;

namespace TransitAnalyticsAPI.Services;

public class VehicleHistoryQueryService : IVehicleHistoryQueryService
{
    private readonly AppDbContext _appDbContext;

    public VehicleHistoryQueryService(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<List<VehicleHistoryPointDto>> GetVehicleHistoryAsync(
        string vehicleId,
        DateTime startUtc,
        DateTime endUtc,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        var positions = await _appDbContext.VehiclePositions
            .AsNoTracking()
            .Where(vehiclePosition =>
                vehiclePosition.VehicleId == vehicleId &&
                vehiclePosition.RecordedAtUtc >= startUtc &&
                vehiclePosition.RecordedAtUtc <= endUtc)
            .OrderBy(vehiclePosition => vehiclePosition.RecordedAtUtc)
            .ThenBy(vehiclePosition => vehiclePosition.Id)
            .Select(vehiclePosition => new VehiclePositionQueryRow
            {
                VehicleId = vehiclePosition.VehicleId,
                TripId = vehiclePosition.TripId,
                RouteId = vehiclePosition.RouteId,
                Latitude = vehiclePosition.Latitude,
                Longitude = vehiclePosition.Longitude,
                Bearing = vehiclePosition.Bearing,
                Speed = vehiclePosition.Speed,
                RecordedAtUtc = vehiclePosition.RecordedAtUtc,
                IngestedAtUtc = vehiclePosition.IngestedAtUtc
            })
            .Take(maxResults + 1)
            .ToListAsync(cancellationToken);

        return await EnrichAsync(positions, cancellationToken);
    }

    public async Task<List<VehicleHistoryPointDto>> GetRangeAsync(
        DateTime startUtc,
        DateTime endUtc,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        var positions = await _appDbContext.VehiclePositions
            .AsNoTracking()
            .Where(vehiclePosition =>
                vehiclePosition.RecordedAtUtc >= startUtc &&
                vehiclePosition.RecordedAtUtc <= endUtc)
            .OrderBy(vehiclePosition => vehiclePosition.RecordedAtUtc)
            .ThenBy(vehiclePosition => vehiclePosition.Id)
            .Select(vehiclePosition => new VehiclePositionQueryRow
            {
                VehicleId = vehiclePosition.VehicleId,
                TripId = vehiclePosition.TripId,
                RouteId = vehiclePosition.RouteId,
                Latitude = vehiclePosition.Latitude,
                Longitude = vehiclePosition.Longitude,
                Bearing = vehiclePosition.Bearing,
                Speed = vehiclePosition.Speed,
                RecordedAtUtc = vehiclePosition.RecordedAtUtc,
                IngestedAtUtc = vehiclePosition.IngestedAtUtc
            })
            .Take(maxResults + 1)
            .ToListAsync(cancellationToken);

        return await EnrichAsync(positions, cancellationToken);
    }

    private async Task<List<VehicleHistoryPointDto>> EnrichAsync(
        List<VehiclePositionQueryRow> positions,
        CancellationToken cancellationToken)
    {
        var activeImportRunId = await _appDbContext.GtfsImportRuns
            .AsNoTracking()
            .Where(importRun => importRun.IsActive && importRun.Status == "completed")
            .OrderByDescending(importRun => importRun.CompletedAtUtc)
            .Select(importRun => (long?)importRun.Id)
            .FirstOrDefaultAsync(cancellationToken);

        Dictionary<string, GtfsTrip> tripsByTripId = new(StringComparer.Ordinal);
        Dictionary<string, GtfsRoute> routesByRouteId = new(StringComparer.Ordinal);

        if (activeImportRunId.HasValue)
        {
            var tripIds = positions
                .Where(position => string.IsNullOrWhiteSpace(position.TripId) == false)
                .Select(position => position.TripId!)
                .Distinct()
                .ToList();

            var trips = await _appDbContext.GtfsTrips
                .AsNoTracking()
                .Where(trip => trip.ImportRunId == activeImportRunId.Value && tripIds.Contains(trip.TripId))
                .ToListAsync(cancellationToken);

            tripsByTripId = trips.ToDictionary(trip => trip.TripId, StringComparer.Ordinal);

            var routeIds = positions
                .Select(position => position.RouteId)
                .Where(routeId => string.IsNullOrWhiteSpace(routeId) == false)
                .Select(routeId => routeId!)
                .Concat(trips.Select(trip => trip.RouteId))
                .Distinct()
                .ToList();

            var routes = await _appDbContext.GtfsRoutes
                .AsNoTracking()
                .Where(route => route.ImportRunId == activeImportRunId.Value && routeIds.Contains(route.RouteId))
                .ToListAsync(cancellationToken);

            routesByRouteId = routes.ToDictionary(route => route.RouteId, StringComparer.Ordinal);
        }

        return positions
            .Select(position =>
            {
                tripsByTripId.TryGetValue(position.TripId ?? string.Empty, out var trip);
                var routeId = position.RouteId ?? trip?.RouteId;
                var route = routeId is not null && routesByRouteId.TryGetValue(routeId, out var resolvedRoute)
                    ? resolvedRoute
                    : null;

                return new VehicleHistoryPointDto
                {
                    VehicleId = position.VehicleId,
                    TripId = position.TripId,
                    RouteId = routeId,
                    Latitude = position.Latitude,
                    Longitude = position.Longitude,
                    Bearing = position.Bearing,
                    Speed = position.Speed,
                    RecordedAtUtc = position.RecordedAtUtc,
                    IngestedAtUtc = position.IngestedAtUtc,
                    RouteShortName = route?.RouteShortName,
                    RouteLongName = route?.RouteLongName,
                    RouteType = route?.RouteType,
                    VehicleType = VehicleLatestQueryService.MapVehicleType(route?.RouteType),
                    RouteColor = route?.RouteColor,
                    TripHeadsign = trip?.TripHeadsign,
                    DirectionId = trip?.DirectionId
                };
            })
            .ToList();
    }

    private sealed class VehiclePositionQueryRow
    {
        public string VehicleId { get; set; } = string.Empty;

        public string? TripId { get; set; }

        public string? RouteId { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public string? Bearing { get; set; }

        public double? Speed { get; set; }

        public DateTime RecordedAtUtc { get; set; }

        public DateTime IngestedAtUtc { get; set; }
    }
}
