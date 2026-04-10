namespace TransitAnalyticsAPI.Services;

public interface IActiveImportRunResolver
{
    Task<long?> GetActiveImportRunIdAsync(CancellationToken cancellationToken = default);
}
