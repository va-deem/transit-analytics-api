namespace TransitAnalyticsAPI.Services;

public interface IVehicleMetadataLookupService
{
    Task<VehicleMetadataLookup> BuildAsync(
        IEnumerable<VehicleMetadataKey> keys,
        CancellationToken cancellationToken = default);
}
