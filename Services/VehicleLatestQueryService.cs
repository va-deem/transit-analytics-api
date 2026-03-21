using Microsoft.EntityFrameworkCore;
using TransitAnalyticsAPI.Models.Dto;
using TransitAnalyticsAPI.Persistence;

namespace TransitAnalyticsAPI.Services;

public class VehicleLatestQueryService : IVehicleLatestQueryService
{
    private readonly AppDbContext _appDbContext;
    private readonly IVehicleMetadataLookupService _vehicleMetadataLookupService;

    public VehicleLatestQueryService(
        AppDbContext appDbContext,
        IVehicleMetadataLookupService vehicleMetadataLookupService)
    {
        _appDbContext = appDbContext;
        _vehicleMetadataLookupService = vehicleMetadataLookupService;
    }

    public async Task<List<VehicleLatestDto>> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        var latestPositions = await _appDbContext.VehiclePositions
            .AsNoTracking()
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
