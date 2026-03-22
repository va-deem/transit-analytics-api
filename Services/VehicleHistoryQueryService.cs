using Microsoft.EntityFrameworkCore;
using TransitAnalyticsAPI.Models.Dto;
using TransitAnalyticsAPI.Persistence;

namespace TransitAnalyticsAPI.Services;

public class VehicleHistoryQueryService : IVehicleHistoryQueryService
{
    private readonly AppDbContext _appDbContext;
    private readonly IVehicleMetadataLookupService _vehicleMetadataLookupService;

    public VehicleHistoryQueryService(
        AppDbContext appDbContext,
        IVehicleMetadataLookupService vehicleMetadataLookupService)
    {
        _appDbContext = appDbContext;
        _vehicleMetadataLookupService = vehicleMetadataLookupService;
    }

    public async Task<List<VehicleHistoryPointDto>> GetVehicleHistoryAsync(
        string vehicleId,
        DateTime startUtc,
        DateTime endUtc,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        var positions = await _appDbContext.VehiclePositions
            .AsNoTracking()
            .Where(vehiclePosition =>
                vehiclePosition.VehicleId == vehicleId &&
                vehiclePosition.RecordedAtUtc >= startUtc &&
                vehiclePosition.RecordedAtUtc <= endUtc)
            .OrderBy(vehiclePosition => vehiclePosition.RecordedAtUtc)
            .ThenBy(vehiclePosition => vehiclePosition.Id)
            .Select(vehiclePosition => new VehiclePositionQueryRow
            {
                VehicleId = vehiclePosition.VehicleId,
                TripId = vehiclePosition.TripId,
                RouteId = vehiclePosition.RouteId,
                Latitude = vehiclePosition.Latitude,
                Longitude = vehiclePosition.Longitude,
                Bearing = vehiclePosition.Bearing,
                Speed = vehiclePosition.Speed,
                RecordedAtUtc = vehiclePosition.RecordedAtUtc,
                IngestedAtUtc = vehiclePosition.IngestedAtUtc
            })
            .Take(maxResults + 1)
            .ToListAsync(cancellationToken);

        return await EnrichAsync(positions, cancellationToken);
    }

    public async Task<List<VehicleHistoryPointDto>> GetRangeAsync(
        DateTime startUtc,
        DateTime endUtc,
        string? routeId,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        var positions = await _appDbContext.VehiclePositions
            .AsNoTracking()
            .Where(vehiclePosition =>
                vehiclePosition.RecordedAtUtc >= startUtc &&
                vehiclePosition.RecordedAtUtc <= endUtc &&
                (string.IsNullOrWhiteSpace(routeId) || vehiclePosition.RouteId == routeId))
            .OrderBy(vehiclePosition => vehiclePosition.RecordedAtUtc)
            .ThenBy(vehiclePosition => vehiclePosition.Id)
            .Select(vehiclePosition => new VehiclePositionQueryRow
            {
                VehicleId = vehiclePosition.VehicleId,
                TripId = vehiclePosition.TripId,
                RouteId = vehiclePosition.RouteId,
                Latitude = vehiclePosition.Latitude,
                Longitude = vehiclePosition.Longitude,
                Bearing = vehiclePosition.Bearing,
                Speed = vehiclePosition.Speed,
                RecordedAtUtc = vehiclePosition.RecordedAtUtc,
                IngestedAtUtc = vehiclePosition.IngestedAtUtc
            })
            .Take(maxResults + 1)
            .ToListAsync(cancellationToken);

        return await EnrichAsync(positions, cancellationToken);
    }

    private async Task<List<VehicleHistoryPointDto>> EnrichAsync(
        List<VehiclePositionQueryRow> positions,
        CancellationToken cancellationToken)
    {
        var lookup = await _vehicleMetadataLookupService.BuildAsync(
            positions.Select(position => new VehicleMetadataKey
            {
                TripId = position.TripId,
                RouteId = position.RouteId
            }),
            cancellationToken);

        return positions
            .Select(position =>
            {
                var metadata = lookup.Resolve(position.TripId, position.RouteId);
                var trip = metadata.Trip;
                var route = metadata.Route;

                return new VehicleHistoryPointDto
                {
                    VehicleId = position.VehicleId,
                    TripId = position.TripId,
                    RouteId = metadata.RouteId,
                    Latitude = position.Latitude,
                    Longitude = position.Longitude,
                    Bearing = position.Bearing,
                    Speed = position.Speed,
                    RecordedAtUtc = position.RecordedAtUtc,
                    IngestedAtUtc = position.IngestedAtUtc,
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

    private sealed class VehiclePositionQueryRow
    {
        public string VehicleId { get; set; } = string.Empty;

        public string? TripId { get; set; }

        public string? RouteId { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public string? Bearing { get; set; }

        public double? Speed { get; set; }

        public DateTime RecordedAtUtc { get; set; }

        public DateTime IngestedAtUtc { get; set; }
    }
}
