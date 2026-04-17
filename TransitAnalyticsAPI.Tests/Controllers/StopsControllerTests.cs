using Microsoft.AspNetCore.Mvc;
using TransitAnalyticsAPI.Controllers;
using TransitAnalyticsAPI.Models.Dto;
using TransitAnalyticsAPI.Services;
using Xunit;

namespace TransitAnalyticsAPI.Tests.Controllers;

public class StopsControllerTests
{
    [Fact]
    public async Task GetDepartures_ReturnsOk_WithDeparturesFromService()
    {
        var departures = new List<StopDepartureDto>
        {
            new() { RouteShortName = "NX1" }
        };
        var controller = new StopsController(new StubStopDeparturesQueryService(departures));

        var result = await controller.GetDepartures("stop-1", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<List<StopDepartureDto>>(ok.Value);
        var departure = Assert.Single(returned);
        Assert.Equal("NX1", departure.RouteShortName);
    }

    [Fact]
    public async Task GetDepartures_ReturnsBadRequest_WhenStopIdIsEmpty()
    {
        var controller = new StopsController(new StubStopDeparturesQueryService([]));

        var result = await controller.GetDepartures("", CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    private sealed class StubStopDeparturesQueryService : IStopDeparturesQueryService
    {
        private readonly List<StopDepartureDto> _departures;

        public StubStopDeparturesQueryService(List<StopDepartureDto> departures)
        {
            _departures = departures;
        }

        public Task<List<StopDepartureDto>> GetUpcomingDeparturesAsync(
            string stopId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_departures);
        }
    }
}
