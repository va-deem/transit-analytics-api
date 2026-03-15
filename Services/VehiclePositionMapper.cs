using TransitAnalyticsAPI.Clients.AucklandTransport.Models;
using TransitAnalyticsAPI.Models.Entities;

namespace TransitAnalyticsAPI.Services;

public class VehiclePositionMapper : IVehiclePositionMapper
{
    public List<VehiclePosition> Map(IEnumerable<AucklandTransportFeedEntity> entities)
    {
        var ingestedAtUtc = DateTime.UtcNow;

        return entities
            .Where(entity =>
                entity.IsDeleted == false &&
                entity.Vehicle?.Vehicle is not null &&
                entity.Vehicle.Position is not null)
            .Select(entity => new VehiclePosition
            {
                VehicleId = entity.Vehicle!.Vehicle!.Id,
                TripId = entity.Vehicle.Trip?.TripId,
                RouteId = entity.Vehicle.Trip?.RouteId,
                Latitude = entity.Vehicle.Position!.Latitude,
                Longitude = entity.Vehicle.Position.Longitude,
                Speed = entity.Vehicle.Position.Speed,
                RecordedAtUtc = DateTimeOffset
                    .FromUnixTimeSeconds(entity.Vehicle.Timestamp)
                    .UtcDateTime,
                IngestedAtUtc = ingestedAtUtc,
                SourceEntityId = entity.Id
            })
            .ToList();
    }
}
