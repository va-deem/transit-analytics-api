using Microsoft.AspNetCore.Mvc;
using TransitAnalyticsAPI.Clients.AucklandTransport;

namespace TransitAnalyticsAPI.Controllers;

[ApiController]
[Route("debug")]
public class  DebugController : ControllerBase
{
    private readonly IAucklandTransportClient _aucklandTransportClient;

    public DebugController(IAucklandTransportClient aucklandTransportClient)
    {
        _aucklandTransportClient = aucklandTransportClient;
    }

    [HttpGet("vehiclelocations")]
    public async Task<IActionResult> GetVehicleLocations(CancellationToken cancellationToken)
    {
        var response = await _aucklandTransportClient.GetVehicleLocationsAsync(cancellationToken);

        if (response?.Response is null)
        {
            return StatusCode(StatusCodes.Status502BadGateway, "Auckland Transport response was empty.");
        }

        var sample = response.Response.Entity
            .Where(entity => entity.Vehicle?.Position is not null)
            .Take(5)
            .Select(entity => new
            {
                entity.Id,
                VehicleId = entity.Vehicle?.Vehicle?.Id,
                TripId = entity.Vehicle?.Trip?.TripId,
                RouteId = entity.Vehicle?.Trip?.RouteId,
                Latitude = entity.Vehicle?.Position?.Latitude,
                Longitude = entity.Vehicle?.Position?.Longitude,
                Speed = entity.Vehicle?.Position?.Speed,
                Timestamp = entity.Vehicle?.Timestamp
            });

        return Ok(new
        {
            response.Status,
            HeaderTimestamp = response.Response.Header?.Timestamp,
            TotalEntities = response.Response.Entity.Count,
            Sample = sample
        });
    }
}
