namespace TransitAnalyticsAPI.Services;

public interface IVehiclePositionIngestionService
{
    Task<VehiclePositionIngestionResult> IngestAsync(CancellationToken cancellationToken = default);
}
