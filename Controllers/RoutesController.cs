using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransitAnalyticsAPI.Models.Dto;
using TransitAnalyticsAPI.Persistence;
using TransitAnalyticsAPI.Services;

namespace TransitAnalyticsAPI.Controllers;

[ApiController]
[Route("routes")]
public class RoutesController : ControllerBase
{
    private readonly AppDbContext _appDbContext;
    private readonly IVehicleLatestQueryService _vehicleLatestQueryService;

    public RoutesController(
        AppDbContext appDbContext,
        IVehicleLatestQueryService vehicleLatestQueryService)
    {
        _appDbContext = appDbContext;
        _vehicleLatestQueryService = vehicleLatestQueryService;
    }

    [HttpGet]
    public async Task<ActionResult<List<RouteDto>>> GetRoutes(CancellationToken cancellationToken)
    {
        var activeImportRunId = await _appDbContext.GtfsImportRuns
            .AsNoTracking()
            .Where(importRun => importRun.IsActive && importRun.Status == "completed")
            .OrderByDescending(importRun => importRun.CompletedAtUtc)
            .Select(importRun => (long?)importRun.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!activeImportRunId.HasValue)
        {
            return Ok(new List<RouteDto>());
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
                VehicleType = VehicleLatestQueryService.MapVehicleType(route.RouteType),
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

        return Ok(routes);
    }

    [HttpGet("{id}/vehicles/latest")]
    public async Task<ActionResult<List<VehicleLatestDto>>> GetLatestVehiclesForRoute(
        string id,
        CancellationToken cancellationToken)
    {
        var latestVehicles = await _vehicleLatestQueryService.GetLatestAsync(cancellationToken);
        var routeVehicles = latestVehicles
            .Where(vehicle => string.Equals(vehicle.RouteId, id, StringComparison.Ordinal))
            .ToList();

        return Ok(routeVehicles);
    }

    [HttpGet("{id}/shape")]
    public async Task<ActionResult<List<RouteShapePointDto>>> GetRouteShape(
        string id,
        CancellationToken cancellationToken)
    {
        var activeImportRunId = await _appDbContext.GtfsImportRuns
            .AsNoTracking()
            .Where(importRun => importRun.IsActive && importRun.Status == "completed")
            .OrderByDescending(importRun => importRun.CompletedAtUtc)
            .Select(importRun => (long?)importRun.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!activeImportRunId.HasValue)
        {
            return Ok(new List<RouteShapePointDto>());
        }

        var shapeId = await _appDbContext.GtfsTrips
            .AsNoTracking()
            .Where(trip => trip.ImportRunId == activeImportRunId.Value && trip.RouteId == id && trip.ShapeId != null)
            .OrderBy(trip => trip.TripId)
            .Select(trip => trip.ShapeId)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(shapeId))
        {
            return Ok(new List<RouteShapePointDto>());
        }

        var shapePoints = await _appDbContext.GtfsShapePoints
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

        return Ok(shapePoints);
    }

    [HttpGet("{id}/stops")]
    public async Task<ActionResult<List<RouteStopDto>>> GetRouteStops(
        string id,
        CancellationToken cancellationToken)
    {
        var activeImportRunId = await _appDbContext.GtfsImportRuns
            .AsNoTracking()
            .Where(importRun => importRun.IsActive && importRun.Status == "completed")
            .OrderByDescending(importRun => importRun.CompletedAtUtc)
            .Select(importRun => (long?)importRun.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!activeImportRunId.HasValue)
        {
            return Ok(new List<RouteStopDto>());
        }

        var tripId = await _appDbContext.GtfsTrips
            .AsNoTracking()
            .Where(trip => trip.ImportRunId == activeImportRunId.Value && trip.RouteId == id)
            .OrderBy(trip => trip.TripId)
            .Select(trip => trip.TripId)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(tripId))
        {
            return Ok(new List<RouteStopDto>());
        }

        var stops = await (
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

        return Ok(stops);
    }
}
