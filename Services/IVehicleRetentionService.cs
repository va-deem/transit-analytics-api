namespace TransitAnalyticsAPI.Services;

public interface IVehicleRetentionService
{
    Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default);
}
