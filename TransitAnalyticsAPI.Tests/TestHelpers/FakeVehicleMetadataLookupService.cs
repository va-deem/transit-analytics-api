using TransitAnalyticsAPI.Services;

namespace TransitAnalyticsAPI.Tests.TestHelpers;

internal sealed class FakeVehicleMetadataLookupService : IVehicleMetadataLookupService
{
    private readonly VehicleMetadataLookup _lookup;

    public FakeVehicleMetadataLookupService(VehicleMetadataLookup? lookup = null)
    {
        _lookup = lookup ?? VehicleMetadataLookup.Empty;
    }

    public Task<VehicleMetadataLookup> BuildAsync(
        IEnumerable<VehicleMetadataKey> keys,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_lookup);
    }
}
