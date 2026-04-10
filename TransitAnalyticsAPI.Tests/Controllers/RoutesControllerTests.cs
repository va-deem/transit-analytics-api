using Microsoft.AspNetCore.Mvc;
using TransitAnalyticsAPI.Controllers;
using TransitAnalyticsAPI.Models.Dto;
using TransitAnalyticsAPI.Services;
using Xunit;

namespace TransitAnalyticsAPI.Tests.Controllers;

public class RoutesControllerTests
{
    [Fact]
    public async Task GetRoutes_ReturnsOk_WithRoutesFromService()
    {
        var routes = new List<RouteDto>
        {
            new() { RouteId = "route-1", RouteShortName = "R1", LatestVehicleCount = 3 },
            new() { RouteId = "route-2", RouteShortName = "R2", LatestVehicleCount = 0 }
        };
        var controller = CreateController(routes: routes);

        var result = await controller.GetRoutes(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<List<RouteDto>>(ok.Value);
        Assert.Equal(2, returned.Count);
        Assert.Equal("route-1", returned[0].RouteId);
    }

    [Fact]
    public async Task GetRoutes_ReturnsOk_WhenServiceReturnsEmpty()
    {
        var controller = CreateController();

        var result = await controller.GetRoutes(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<List<RouteDto>>(ok.Value);
        Assert.Empty(returned);
    }

    [Fact]
    public async Task GetLatestVehiclesForRoute_FiltersVehiclesByRouteId()
    {
        var vehicles = new List<VehicleLatestDto>
        {
            new() { VehicleId = "v1", RouteId = "route-1" },
            new() { VehicleId = "v2", RouteId = "route-2" },
            new() { VehicleId = "v3", RouteId = "route-1" }
        };
        var controller = CreateController(latestVehicles: vehicles);

        var result = await controller.GetLatestVehiclesForRoute("route-1", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<List<VehicleLatestDto>>(ok.Value);
        Assert.Equal(2, returned.Count);
        Assert.All(returned, v => Assert.Equal("route-1", v.RouteId));
    }

    [Fact]
    public async Task GetLatestVehiclesForRoute_ReturnsEmpty_WhenNoVehiclesMatchRoute()
    {
        var vehicles = new List<VehicleLatestDto>
        {
            new() { VehicleId = "v1", RouteId = "route-1" }
        };
        var controller = CreateController(latestVehicles: vehicles);

        var result = await controller.GetLatestVehiclesForRoute("route-999", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<List<VehicleLatestDto>>(ok.Value);
        Assert.Empty(returned);
    }

    [Fact]
    public async Task GetRouteShape_ReturnsOk_WithShapePointsFromService()
    {
        var shapePoints = new List<RouteShapePointDto>
        {
            new() { ShapeId = "shape-1", Latitude = -36.8, Longitude = 174.7, Sequence = 1 },
            new() { ShapeId = "shape-1", Latitude = -36.9, Longitude = 174.8, Sequence = 2 }
        };
        var controller = CreateController(shapePoints: shapePoints);

        var result = await controller.GetRouteShape("route-1", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<List<RouteShapePointDto>>(ok.Value);
        Assert.Equal(2, returned.Count);
    }

    [Fact]
    public async Task GetRouteShape_ReturnsOk_WhenServiceReturnsEmpty()
    {
        var controller = CreateController();

        var result = await controller.GetRouteShape("route-1", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<List<RouteShapePointDto>>(ok.Value);
        Assert.Empty(returned);
    }

    [Fact]
    public async Task GetRouteStops_ReturnsOk_WithStopsFromService()
    {
        var stops = new List<RouteStopDto>
        {
            new() { StopId = "stop-1", StopName = "First Stop", StopSequence = 1 },
            new() { StopId = "stop-2", StopName = "Second Stop", StopSequence = 2 }
        };
        var controller = CreateController(stops: stops);

        var result = await controller.GetRouteStops("route-1", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<List<RouteStopDto>>(ok.Value);
        Assert.Equal(2, returned.Count);
        Assert.Equal("First Stop", returned[0].StopName);
    }

    [Fact]
    public async Task GetRouteStops_ReturnsOk_WhenServiceReturnsEmpty()
    {
        var controller = CreateController();

        var result = await controller.GetRouteStops("route-1", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<List<RouteStopDto>>(ok.Value);
        Assert.Empty(returned);
    }

    private static RoutesController CreateController(
        List<RouteDto>? routes = null,
        List<VehicleLatestDto>? latestVehicles = null,
        List<RouteShapePointDto>? shapePoints = null,
        List<RouteStopDto>? stops = null)
    {
        return new RoutesController(
            new StubRoutesQueryService(routes, shapePoints, stops),
            new StubVehicleLatestQueryService(latestVehicles));
    }

    private sealed class StubRoutesQueryService : IRoutesQueryService
    {
        private readonly List<RouteDto> _routes;
        private readonly List<RouteShapePointDto> _shapePoints;
        private readonly List<RouteStopDto> _stops;

        public StubRoutesQueryService(
            List<RouteDto>? routes = null,
            List<RouteShapePointDto>? shapePoints = null,
            List<RouteStopDto>? stops = null)
        {
            _routes = routes ?? [];
            _shapePoints = shapePoints ?? [];
            _stops = stops ?? [];
        }

        public Task<List<RouteDto>> GetRoutesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_routes);
        }

        public Task<List<RouteShapePointDto>> GetRouteShapeAsync(
            string routeId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_shapePoints);
        }

        public Task<List<RouteStopDto>> GetRouteStopsAsync(
            string routeId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_stops);
        }
    }

    private sealed class StubVehicleLatestQueryService : IVehicleLatestQueryService
    {
        private readonly List<VehicleLatestDto> _vehicles;

        public StubVehicleLatestQueryService(List<VehicleLatestDto>? vehicles = null)
        {
            _vehicles = vehicles ?? [];
        }

        public Task<List<VehicleLatestDto>> GetLatestAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_vehicles);
        }
    }
}
