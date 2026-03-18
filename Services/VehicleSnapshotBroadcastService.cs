namespace TransitAnalyticsAPI.Services;

public class VehicleSnapshotBroadcastService : IVehicleSnapshotBroadcastService
{
    private readonly IWebSocketSubscriptionManager _subscriptionManager;
    private readonly IVehicleWebSocketService _vehicleWebSocketService;

    public VehicleSnapshotBroadcastService(
        IWebSocketSubscriptionManager subscriptionManager,
        IVehicleWebSocketService vehicleWebSocketService)
    {
        _subscriptionManager = subscriptionManager;
        _vehicleWebSocketService = vehicleWebSocketService;
    }

    public async Task BroadcastAsync(CancellationToken cancellationToken = default)
    {
        var channels = _subscriptionManager.GetSubscribedChannels();

        foreach (var channel in channels)
        {
            var subscribers = _subscriptionManager.GetSubscribers(channel);

            foreach (var subscriber in subscribers)
            {
                await _vehicleWebSocketService.SendSnapshotAsync(subscriber, channel, cancellationToken);
            }
        }
    }
}
