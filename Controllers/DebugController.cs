using Microsoft.AspNetCore.Mvc;
using TransitAnalyticsAPI.Clients.AucklandTransport;
using TransitAnalyticsAPI.Services;

namespace TransitAnalyticsAPI.Controllers;

[ApiController]
[Route("debug")]
public class DebugController : ControllerBase
{
    private readonly IAucklandTransportClient _aucklandTransportClient;
    private readonly IVehiclePositionIngestionService _vehiclePositionIngestionService;
    private readonly IVehiclePositionMapper _vehiclePositionMapper;

    public DebugController(
        IAucklandTransportClient aucklandTransportClient,
        IVehiclePositionIngestionService vehiclePositionIngestionService,
        IVehiclePositionMapper vehiclePositionMapper)
    {
        _aucklandTransportClient = aucklandTransportClient;
        _vehiclePositionIngestionService = vehiclePositionIngestionService;
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

    [HttpPost("vehiclelocations/save")]
    public async Task<IActionResult> SaveVehicleLocations(CancellationToken cancellationToken)
    {
        var result = await _vehiclePositionIngestionService.IngestAsync(cancellationToken);

        return Ok(result);
    }
}
