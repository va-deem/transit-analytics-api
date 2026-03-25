using Microsoft.AspNetCore.Mvc;
using TransitAnalyticsAPI.Controllers;
using TransitAnalyticsAPI.Models.Dto;
using TransitAnalyticsAPI.Services;
using Xunit;

namespace TransitAnalyticsAPI.Tests.Controllers;

public class VehiclesControllerTests
{
    [Fact]
    public async Task GetHistory_ReturnsBadRequest_WhenStartOrEndIsMissing()
    {
        var controller = new VehiclesController(
            new StubVehicleLatestQueryService(),
            new StubVehicleHistoryQueryService());

        var result = await controller.GetHistory("vehicle-1", null, DateTimeOffset.UtcNow, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task GetHistory_ReturnsBadRequest_WhenEndIsBeforeStart()
    {
        var controller = new VehiclesController(
            new StubVehicleLatestQueryService(),
            new StubVehicleHistoryQueryService());

        var start = DateTimeOffset.UtcNow;
        var end = start.AddMinutes(-1);

        var result = await controller.GetHistory("vehicle-1", start, end, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task GetRange_ReturnsBadRequest_WhenWindowExceedsSixHours()
    {
        var controller = new VehiclesController(
            new StubVehicleLatestQueryService(),
            new StubVehicleHistoryQueryService());

        var start = DateTimeOffset.UtcNow;
        var end = start.AddHours(6).AddMinutes(1);

        var result = await controller.GetRange(start, end, null, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    private sealed class StubVehicleLatestQueryService : IVehicleLatestQueryService
    {
        public Task<List<VehicleLatestDto>> GetLatestAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<VehicleLatestDto>());
        }
    }

    private sealed class StubVehicleHistoryQueryService : IVehicleHistoryQueryService
    {
        public Task<List<VehicleHistoryPointDto>> GetVehicleHistoryAsync(
            string vehicleId,
            DateTime startUtc,
            DateTime endUtc,
            int maxResults,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<VehicleHistoryPointDto>());
        }

        public Task<List<VehicleHistoryPointDto>> GetRangeAsync(
            DateTime startUtc,
            DateTime endUtc,
            string? routeId,
            int maxResults,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<VehicleHistoryPointDto>());
        }
    }
}
