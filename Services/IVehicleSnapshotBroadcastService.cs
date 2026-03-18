namespace TransitAnalyticsAPI.Services;

public interface IVehicleSnapshotBroadcastService
{
    Task BroadcastAsync(CancellationToken cancellationToken = default);
}
