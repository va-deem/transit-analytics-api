using Microsoft.AspNetCore.Mvc;
using TransitAnalyticsAPI.Models.Dto;
using TransitAnalyticsAPI.Services;

namespace TransitAnalyticsAPI.Controllers;

[ApiController]
[Route("vehicles")]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleLatestQueryService _vehicleLatestQueryService;

    public VehiclesController(IVehicleLatestQueryService vehicleLatestQueryService)
    {
        _vehicleLatestQueryService = vehicleLatestQueryService;
    }

    [HttpGet("latest")]
    public async Task<ActionResult<List<VehicleLatestDto>>> GetLatest(CancellationToken cancellationToken)
    {
        var latestVehicles = await _vehicleLatestQueryService.GetLatestAsync(cancellationToken);

        return Ok(latestVehicles);
    }
}
