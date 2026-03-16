namespace TransitAnalyticsAPI.Services;

public interface IGtfsImportService
{
    Task<GtfsImportResult> ImportRoutesAndTripsAsync(CancellationToken cancellationToken = default);
}
