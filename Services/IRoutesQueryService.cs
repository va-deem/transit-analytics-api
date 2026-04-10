using TransitAnalyticsAPI.Models.Dto;

namespace TransitAnalyticsAPI.Services;

public interface IRoutesQueryService
{
    Task<List<RouteDto>> GetRoutesAsync(CancellationToken cancellationToken = default);

    Task<List<RouteShapePointDto>> GetRouteShapeAsync(string routeId, CancellationToken cancellationToken = default);

    Task<List<RouteStopDto>> GetRouteStopsAsync(string routeId, CancellationToken cancellationToken = default);
}
