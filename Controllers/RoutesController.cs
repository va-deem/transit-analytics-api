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
}
