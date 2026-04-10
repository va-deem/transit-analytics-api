using Microsoft.EntityFrameworkCore;
using TransitAnalyticsAPI.Models.Dto;
using TransitAnalyticsAPI.Persistence;

namespace TransitAnalyticsAPI.Services;

public class RoutesQueryService : IRoutesQueryService
{
    private readonly AppDbContext _appDbContext;
    private readonly IVehicleLatestQueryService _vehicleLatestQueryService;

    public RoutesQueryService(
        AppDbContext appDbContext,
        IVehicleLatestQueryService vehicleLatestQueryService)
    {
        _appDbContext = appDbContext;
        _vehicleLatestQueryService = vehicleLatestQueryService;
    }

    public async Task<List<RouteDto>> GetRoutesAsync(CancellationToken cancellationToken = default)
    {
        var activeImportRunId = await GetActiveImportRunIdAsync(cancellationToken);

        if (!activeImportRunId.HasValue)
        {
            return [];
        }

        var latestVehicles = await _vehicleLatestQueryService.GetLatestAsync(cancellationToken);
        var latestVehicleCountsByRouteId = latestVehicles
            .Where(vehicle => string.IsNullOrWhiteSpace(vehicle.RouteId) == false)
            .GroupBy(vehicle => vehicle.RouteId!)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        var routes = await _appDbContext.GtfsRoutes
            .AsNoTracking()
            .Where(route => route.ImportRunId == activeImportRunId.Value)
            .OrderBy(route => route.RouteShortName)
            .ThenBy(route => route.RouteLongName)
            .Select(route => new RouteDto
            {
                RouteId = route.RouteId,
                RouteShortName = route.RouteShortName,
                RouteLongName = route.RouteLongName,
                RouteType = route.RouteType,
                VehicleType = VehicleTypeMapper.Map(route.RouteType),
                RouteColor = route.RouteColor,
                LatestVehicleCount = 0
            })
            .ToListAsync(cancellationToken);

        foreach (var route in routes)
        {
            if (latestVehicleCountsByRouteId.TryGetValue(route.RouteId, out var latestVehicleCount))
            {
                route.LatestVehicleCount = latestVehicleCount;
            }
        }

        return routes;
    }

    public async Task<List<RouteShapePointDto>> GetRouteShapeAsync(
        string routeId,
        CancellationToken cancellationToken = default)
    {
        var activeImportRunId = await GetActiveImportRunIdAsync(cancellationToken);

        if (!activeImportRunId.HasValue)
        {
            return [];
        }

        var shapeId = await _appDbContext.GtfsTrips
            .AsNoTracking()
            .Where(trip => trip.ImportRunId == activeImportRunId.Value && trip.RouteId == routeId && trip.ShapeId != null)
            .OrderBy(trip => trip.TripId)
            .Select(trip => trip.ShapeId)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(shapeId))
        {
            return [];
        }

        return await _appDbContext.GtfsShapePoints
            .AsNoTracking()
            .Where(shapePoint => shapePoint.ImportRunId == activeImportRunId.Value && shapePoint.ShapeId == shapeId)
            .OrderBy(shapePoint => shapePoint.Sequence)
            .Select(shapePoint => new RouteShapePointDto
            {
                ShapeId = shapePoint.ShapeId,
                Latitude = shapePoint.Latitude,
                Longitude = shapePoint.Longitude,
                Sequence = shapePoint.Sequence
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RouteStopDto>> GetRouteStopsAsync(
        string routeId,
        CancellationToken cancellationToken = default)
    {
        var activeImportRunId = await GetActiveImportRunIdAsync(cancellationToken);

        if (!activeImportRunId.HasValue)
        {
            return [];
        }

        var tripId = await _appDbContext.GtfsTrips
            .AsNoTracking()
            .Where(trip => trip.ImportRunId == activeImportRunId.Value && trip.RouteId == routeId)
            .OrderBy(trip => trip.TripId)
            .Select(trip => trip.TripId)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(tripId))
        {
            return [];
        }

        return await (
            from stopTime in _appDbContext.GtfsStopTimes.AsNoTracking()
            join stop in _appDbContext.GtfsStops.AsNoTracking()
                on new { stopTime.ImportRunId, stopTime.StopId } equals new { stop.ImportRunId, stop.StopId }
            where stopTime.ImportRunId == activeImportRunId.Value && stopTime.TripId == tripId
            orderby stopTime.StopSequence
            select new RouteStopDto
            {
                StopId = stop.StopId,
                StopCode = stop.StopCode,
                StopName = stop.StopName,
                StopLat = stop.StopLat,
                StopLon = stop.StopLon,
                StopSequence = stopTime.StopSequence,
                PlatformCode = stop.PlatformCode
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<long?> GetActiveImportRunIdAsync(CancellationToken cancellationToken)
    {
        return await _appDbContext.GtfsImportRuns
            .AsNoTracking()
            .Where(importRun => importRun.IsActive && importRun.Status == "completed")
            .OrderByDescending(importRun => importRun.CompletedAtUtc)
            .Select(importRun => (long?)importRun.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
