using TransitAnalyticsAPI.Clients.AucklandTransport.Models;

namespace TransitAnalyticsAPI.Clients.AucklandTransport;

public interface IAucklandTransportClient
{
    Task<AucklandTransportApiResponse?> GetVehicleLocationsAsync(CancellationToken cancellationToken = default);
}
