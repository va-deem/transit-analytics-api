using Microsoft.AspNetCore.Mvc;
using TransitAnalyticsAPI.Clients.AucklandTransport;
using TransitAnalyticsAPI.Services;

namespace TransitAnalyticsAPI.Controllers;

[ApiController]
[Route("debug")]
public class DebugController : ControllerBase
{
    private readonly IAucklandTransportClient _aucklandTransportClient;
    private readonly IVehiclePositionMapper _vehiclePositionMapper;

    public DebugController(
        IAucklandTransportClient aucklandTransportClient,
        IVehiclePositionMapper vehiclePositionMapper)
    {
        _aucklandTransportClient = aucklandTransportClient;
        _vehiclePositionMapper = vehiclePositionMapper;
    }

    [HttpGet("vehiclelocations")]
    public async Task<IActionResult> GetVehicleLocations(CancellationToken cancellationToken)
    {
        var response = await _aucklandTransportClient.GetVehicleLocationsAsync(cancellationToken);

        if (response?.Response is null)
        {
            return StatusCode(StatusCodes.Status502BadGateway, "Auckland Transport response was empty.");
        }

        var vehiclePositions = _vehiclePositionMapper.Map(response.Response.Entity);
        var sample = vehiclePositions.Take(5);

        return Ok(new
        {
            response.Status,
            HeaderTimestamp = response.Response.Header?.Timestamp,
            TotalEntities = response.Response.Entity.Count,
            MappedVehiclePositions = vehiclePositions.Count,
            Sample = sample
        });
    }
}
