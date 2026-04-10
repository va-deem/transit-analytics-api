using Microsoft.AspNetCore.Mvc;
using TransitAnalyticsAPI.Models.Dto;
using TransitAnalyticsAPI.Services;

namespace TransitAnalyticsAPI.Controllers;

[ApiController]
[Route("routes")]
public class RoutesController : ControllerBase
{
    private readonly IRoutesQueryService _routesQueryService;
    private readonly IVehicleLatestQueryService _vehicleLatestQueryService;

    public RoutesController(
        IRoutesQueryService routesQueryService,
        IVehicleLatestQueryService vehicleLatestQueryService)
    {
        _routesQueryService = routesQueryService;
        _vehicleLatestQueryService = vehicleLatestQueryService;
    }

    [HttpGet]
    public async Task<ActionResult<List<RouteDto>>> GetRoutes(CancellationToken cancellationToken)
    {
        var routes = await _routesQueryService.GetRoutesAsync(cancellationToken);
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
        var shapePoints = await _routesQueryService.GetRouteShapeAsync(id, cancellationToken);
        return Ok(shapePoints);
    }

    [HttpGet("{id}/stops")]
    public async Task<ActionResult<List<RouteStopDto>>> GetRouteStops(
        string id,
        CancellationToken cancellationToken)
    {
        var stops = await _routesQueryService.GetRouteStopsAsync(id, cancellationToken);
        return Ok(stops);
    }
}
