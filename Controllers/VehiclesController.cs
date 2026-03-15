using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransitAnalyticsAPI.Models.Dto;
using TransitAnalyticsAPI.Persistence;

namespace TransitAnalyticsAPI.Controllers;

[ApiController]
[Route("vehicles")]
public class VehiclesController : ControllerBase
{
    private readonly AppDbContext _appDbContext;

    public VehiclesController(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    [HttpGet("latest")]
    public async Task<ActionResult<List<VehicleLatestDto>>> GetLatest(CancellationToken cancellationToken)
    {
        var latestVehicles = await _appDbContext.VehiclePositions
            .AsNoTracking()
            .GroupBy(vehiclePosition => vehiclePosition.VehicleId)
            .Select(group => group
                .OrderByDescending(vehiclePosition => vehiclePosition.RecordedAtUtc)
                .ThenByDescending(vehiclePosition => vehiclePosition.Id)
                .Select(vehiclePosition => new VehicleLatestDto
                {
                    VehicleId = vehiclePosition.VehicleId,
                    TripId = vehiclePosition.TripId,
                    RouteId = vehiclePosition.RouteId,
                    Latitude = vehiclePosition.Latitude,
                    Longitude = vehiclePosition.Longitude,
                    Speed = vehiclePosition.Speed,
                    RecordedAtUtc = vehiclePosition.RecordedAtUtc
                })
                .First())
            .ToListAsync(cancellationToken);

        return Ok(latestVehicles);
    }
}
