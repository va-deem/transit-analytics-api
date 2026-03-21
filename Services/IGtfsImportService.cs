namespace TransitAnalyticsAPI.Services;

public interface IGtfsImportService
{
    Task<GtfsImportResult> ImportRoutesAndTripsAsync(string gtfsDirectory, CancellationToken cancellationToken = default);
}
