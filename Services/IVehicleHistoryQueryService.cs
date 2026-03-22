using TransitAnalyticsAPI.Models.Dto;

namespace TransitAnalyticsAPI.Services;

public interface IVehicleHistoryQueryService
{
    Task<List<VehicleHistoryPointDto>> GetVehicleHistoryAsync(
        string vehicleId,
        DateTime startUtc,
        DateTime endUtc,
        int maxResults,
        CancellationToken cancellationToken = default);

    Task<List<VehicleHistoryPointDto>> GetRangeAsync(
        DateTime startUtc,
        DateTime endUtc,
        string? routeId,
        int maxResults,
        CancellationToken cancellationToken = default);
}
