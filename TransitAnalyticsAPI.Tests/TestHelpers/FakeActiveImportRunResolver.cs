using TransitAnalyticsAPI.Services;

namespace TransitAnalyticsAPI.Tests.TestHelpers;

internal sealed class FakeActiveImportRunResolver : IActiveImportRunResolver
{
    private readonly long? _activeImportRunId;

    public FakeActiveImportRunResolver(long? activeImportRunId)
    {
        _activeImportRunId = activeImportRunId;
    }

    public Task<long?> GetActiveImportRunIdAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_activeImportRunId);
    }
}
