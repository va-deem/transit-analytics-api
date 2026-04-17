using Microsoft.AspNetCore.Mvc;
using TransitAnalyticsAPI.Models.Dto;
using TransitAnalyticsAPI.Services;

namespace TransitAnalyticsAPI.Controllers;

[ApiController]
[Route("stops")]
public class StopsController : ControllerBase
{
    private readonly IStopDeparturesQueryService _stopDeparturesQueryService;

    public StopsController(IStopDeparturesQueryService stopDeparturesQueryService)
    {
        _stopDeparturesQueryService = stopDeparturesQueryService;
    }

    [HttpGet("{stopId}/departures")]
    public async Task<ActionResult<List<StopDepartureDto>>> GetDepartures(
        string stopId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(stopId))
        {
            return BadRequest(new
            {
                error = "invalid_stop_id",
                message = "A stop id is required."
            });
        }

        var departures = await _stopDeparturesQueryService.GetUpcomingDeparturesAsync(stopId, cancellationToken);
        return Ok(departures);
    }
}
