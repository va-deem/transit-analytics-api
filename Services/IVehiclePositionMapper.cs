using TransitAnalyticsAPI.Clients.AucklandTransport.Models;
using TransitAnalyticsAPI.Models.Entities;

namespace TransitAnalyticsAPI.Services;

public interface IVehiclePositionMapper
{
    List<VehiclePosition> Map(IEnumerable<AucklandTransportFeedEntity> entities);
}
