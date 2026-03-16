using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransitAnalyticsAPI.Models.Dto;
using TransitAnalyticsAPI.Persistence;

namespace TransitAnalyticsAPI.Controllers;

[ApiController]
[Route("vehicles")]
public class VehiclesController : ControllerBase
{
    private readonly AppDbContext _appDbContext;

    public VehiclesController(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    [HttpGet("latest")]
    public async Task<ActionResult<List<VehicleLatestDto>>> GetLatest(CancellationToken cancellationToken)
    {
        var activeImportRunId = await _appDbContext.GtfsImportRuns
            .AsNoTracking()
            .Where(importRun => importRun.IsActive && importRun.Status == "completed")
            .OrderByDescending(importRun => importRun.CompletedAtUtc)
            .Select(importRun => (long?)importRun.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var latestPositions = await _appDbContext.VehiclePositions
            .AsNoTracking()
            .GroupBy(vehiclePosition => vehiclePosition.VehicleId)
            .Select(group => group
                .OrderByDescending(vehiclePosition => vehiclePosition.RecordedAtUtc)
                .ThenByDescending(vehiclePosition => vehiclePosition.Id)
                .Select(vehiclePosition => new
                {
                    vehiclePosition.VehicleId,
                    vehiclePosition.TripId,
                    vehiclePosition.RouteId,
                    vehiclePosition.Latitude,
                    vehiclePosition.Longitude,
                    vehiclePosition.Speed,
                    vehiclePosition.RecordedAtUtc
                })
                .First())
            .ToListAsync(cancellationToken);

        Dictionary<string, Models.Entities.GtfsTrip> tripsByTripId = new(StringComparer.Ordinal);
        Dictionary<string, Models.Entities.GtfsRoute> routesByRouteId = new(StringComparer.Ordinal);

        if (activeImportRunId.HasValue)
        {
            var tripIds = latestPositions
                .Where(position => string.IsNullOrWhiteSpace(position.TripId) == false)
                .Select(position => position.TripId!)
                .Distinct()
                .ToList();

            var trips = await _appDbContext.GtfsTrips
                .AsNoTracking()
                .Where(trip => trip.ImportRunId == activeImportRunId.Value && tripIds.Contains(trip.TripId))
                .ToListAsync(cancellationToken);

            tripsByTripId = trips.ToDictionary(trip => trip.TripId, StringComparer.Ordinal);

            var routeIds = latestPositions
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

        var latestVehicles = latestPositions
            .Select(position =>
            {
                tripsByTripId.TryGetValue(position.TripId ?? string.Empty, out var trip);
                var routeId = position.RouteId ?? trip?.RouteId;
                var route = routeId is not null && routesByRouteId.TryGetValue(routeId, out var resolvedRoute)
                    ? resolvedRoute
                    : null;

                return new VehicleLatestDto
                {
                    VehicleId = position.VehicleId,
                    TripId = position.TripId,
                    RouteId = routeId,
                    Latitude = position.Latitude,
                    Longitude = position.Longitude,
                    Speed = position.Speed,
                    RecordedAtUtc = position.RecordedAtUtc,
                    RouteShortName = route?.RouteShortName,
                    RouteLongName = route?.RouteLongName,
                    RouteType = route?.RouteType,
                    VehicleType = MapVehicleType(route?.RouteType),
                    RouteColor = route?.RouteColor,
                    TripHeadsign = trip?.TripHeadsign,
                    DirectionId = trip?.DirectionId
                };
            })
            .ToList();

        return Ok(latestVehicles);
    }

    private static string? MapVehicleType(int? routeType)
    {
        return routeType switch
        {
            0 => "tram",
            1 => "subway",
            2 => "train",
            3 => "bus",
            4 => "ferry",
            5 => "cable_tram",
            6 => "aerial_lift",
            7 => "funicular",
            11 => "trolleybus",
            12 => "monorail",
            _ => null
        };
    }
}
