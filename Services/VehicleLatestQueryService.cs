using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TransitAnalyticsAPI.Configuration;
using TransitAnalyticsAPI.Models.Dto;
using TransitAnalyticsAPI.Persistence;

namespace TransitAnalyticsAPI.Services;

public class VehicleLatestQueryService : IVehicleLatestQueryService
{
    private readonly AppDbContext _appDbContext;
    private readonly IVehicleMetadataLookupService _vehicleMetadataLookupService;
    private readonly TimeSpan _latestPositionMaxAge;

    public VehicleLatestQueryService(
        AppDbContext appDbContext,
        IVehicleMetadataLookupService vehicleMetadataLookupService,
        IOptions<VehicleOptions> vehicleOptions)
    {
        _appDbContext = appDbContext;
        _vehicleMetadataLookupService = vehicleMetadataLookupService;
        _latestPositionMaxAge = TimeSpan.FromMinutes(Math.Max(1, vehicleOptions.Value.LatestPositionMaxAgeMinutes));
    }

    public async Task<List<VehicleLatestDto>> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        var cutoffUtc = DateTime.UtcNow - _latestPositionMaxAge;

        var latestPositions = await _appDbContext.VehiclePositions
            .AsNoTracking()
            .Where(vehiclePosition => vehiclePosition.RecordedAtUtc >= cutoffUtc)
            .GroupBy(vehiclePosition => vehiclePosition.VehicleId)
            .Select(group => group
                .OrderByDescending(vehiclePosition => vehiclePosition.RecordedAtUtc)
                .ThenByDescending(vehiclePosition => vehiclePosition.Id)
                .Select(vehiclePosition => new
                {
                    vehiclePosition.VehicleId,
                    vehiclePosition.TripId,
                    vehiclePosition.RouteId,
                    vehiclePosition.Latitude,
                    vehiclePosition.Longitude,
                    vehiclePosition.Speed,
                    vehiclePosition.RecordedAtUtc
                })
                .First())
            .ToListAsync(cancellationToken);

        var lookup = await _vehicleMetadataLookupService.BuildAsync(
            latestPositions.Select(position => new VehicleMetadataKey
            {
                TripId = position.TripId,
                RouteId = position.RouteId
            }),
            cancellationToken);

        return latestPositions
            .Select(position =>
            {
                var metadata = lookup.Resolve(position.TripId, position.RouteId);
                var trip = metadata.Trip;
                var route = metadata.Route;

                return new VehicleLatestDto
                {
                    VehicleId = position.VehicleId,
                    TripId = position.TripId,
                    RouteId = metadata.RouteId,
                    Latitude = position.Latitude,
                    Longitude = position.Longitude,
                    Speed = position.Speed,
                    RecordedAtUtc = position.RecordedAtUtc,
                    RouteShortName = route?.RouteShortName,
                    RouteLongName = route?.RouteLongName,
                    RouteType = route?.RouteType,
                    VehicleType = VehicleTypeMapper.Map(route?.RouteType),
                    RouteColor = route?.RouteColor,
                    TripHeadsign = trip?.TripHeadsign,
                    DirectionId = trip?.DirectionId
                };
            })
            .ToList();
    }
}
