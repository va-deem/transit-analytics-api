using System.Net.WebSockets;

namespace TransitAnalyticsAPI.Services;

public interface IWebSocketSubscriptionManager
{
    Task<bool> AddConnectionAsync(WebSocket socket, string ipAddress, CancellationToken cancellationToken = default);

    Task RemoveConnectionAsync(WebSocket socket, CancellationToken cancellationToken = default);

    Task SubscribeAsync(WebSocket socket, string channel, CancellationToken cancellationToken = default);

    Task UnsubscribeAsync(WebSocket socket, string channel, CancellationToken cancellationToken = default);

    IReadOnlyList<string> GetSubscribedChannels();

    IReadOnlyList<WebSocket> GetSubscribers(string channel);

    int GetConnectionCount();
}
