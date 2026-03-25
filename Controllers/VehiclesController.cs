using Microsoft.AspNetCore.Mvc;
using TransitAnalyticsAPI.Models.Dto;
using TransitAnalyticsAPI.Services;

namespace TransitAnalyticsAPI.Controllers;

[ApiController]
[Route("vehicles")]
public class VehiclesController : ControllerBase
{
    private const int MaxVehicleHistoryPoints = 5_000;
    private const int MaxVehicleRangePoints = 50_000;
    private static readonly TimeSpan MaxVehicleHistorySpan = TimeSpan.FromDays(7);
    private static readonly TimeSpan MaxVehicleRangeSpan = TimeSpan.FromHours(6);

    private readonly IVehicleHistoryQueryService _vehicleHistoryQueryService;
    private readonly IVehicleLatestQueryService _vehicleLatestQueryService;

    public VehiclesController(
        IVehicleLatestQueryService vehicleLatestQueryService,
        IVehicleHistoryQueryService vehicleHistoryQueryService)
    {
        _vehicleLatestQueryService = vehicleLatestQueryService;
        _vehicleHistoryQueryService = vehicleHistoryQueryService;
    }

    [HttpGet("latest")]
    public async Task<ActionResult<List<VehicleLatestDto>>> GetLatest(CancellationToken cancellationToken)
    {
        var latestVehicles = await _vehicleLatestQueryService.GetLatestAsync(cancellationToken);

        return Ok(latestVehicles);
    }

    [HttpGet("{id}/history")]
    public async Task<ActionResult<List<VehicleHistoryPointDto>>> GetHistory(
        string id,
        [FromQuery] DateTimeOffset? start,
        [FromQuery] DateTimeOffset? end,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new
            {
                error = "invalid_vehicle_id",
                message = "A vehicle id is required."
            });
        }

        var validationError = ValidateTimeRange(start, end, MaxVehicleHistorySpan, "history");
        if (validationError is not null)
        {
            return validationError;
        }

        var history = await _vehicleHistoryQueryService.GetVehicleHistoryAsync(
            id,
            start!.Value.UtcDateTime,
            end!.Value.UtcDateTime,
            MaxVehicleHistoryPoints,
            cancellationToken);

        if (history.Count > MaxVehicleHistoryPoints)
        {
            return BadRequest(new
            {
                error = "history_result_limit_exceeded",
                message = $"The requested history window returned more than {MaxVehicleHistoryPoints} points. Narrow the time range."
            });
        }

        return Ok(history);
    }

    [HttpGet("range")]
    public async Task<ActionResult<List<VehicleHistoryPointDto>>> GetRange(
        [FromQuery] DateTimeOffset? start,
        [FromQuery] DateTimeOffset? end,
        [FromQuery] string? routeId,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateTimeRange(start, end, MaxVehicleRangeSpan, "range");
        if (validationError is not null)
        {
            return validationError;
        }

        var history = await _vehicleHistoryQueryService.GetRangeAsync(
            start!.Value.UtcDateTime,
            end!.Value.UtcDateTime,
            routeId,
            MaxVehicleRangePoints,
            cancellationToken);

        if (history.Count > MaxVehicleRangePoints)
        {
            return BadRequest(new
            {
                error = "range_result_limit_exceeded",
                message = $"The requested playback window returned more than {MaxVehicleRangePoints} points. Narrow the time range."
            });
        }

        return Ok(history);
    }

    private BadRequestObjectResult? ValidateTimeRange(
        DateTimeOffset? start,
        DateTimeOffset? end,
        TimeSpan maxSpan,
        string queryName)
    {
        if (!start.HasValue || !end.HasValue)
        {
            return BadRequest(new
            {
                error = "missing_time_range",
                message = $"Both start and end query parameters are required for {queryName} queries."
            });
        }

        if (end.Value < start.Value)
        {
            return BadRequest(new
            {
                error = "invalid_time_range",
                message = "The end timestamp must be greater than or equal to the start timestamp."
            });
        }

        if (end.Value - start.Value > maxSpan)
        {
            return BadRequest(new
            {
                error = "time_range_too_large",
                message = $"The requested time range exceeds the maximum allowed window of {maxSpan.TotalHours:0.#} hours."
            });
        }

        return null;
    }
}
