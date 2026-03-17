using TransitAnalyticsAPI.Models.Dto;

namespace TransitAnalyticsAPI.Services;

public interface IVehicleLatestQueryService
{
    Task<List<VehicleLatestDto>> GetLatestAsync(CancellationToken cancellationToken = default);
}
