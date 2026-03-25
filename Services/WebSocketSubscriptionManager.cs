using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace TransitAnalyticsAPI.Services;

public class WebSocketSubscriptionManager : IWebSocketSubscriptionManager
{
    private readonly ConcurrentDictionary<WebSocket, string?> _subscriptions = new();
    private readonly int _maxConcurrentConnections;

    public WebSocketSubscriptionManager(Microsoft.Extensions.Options.IOptions<TransitAnalyticsAPI.Configuration.VehicleWebSocketOptions> options)
    {
        _maxConcurrentConnections = Math.Max(1, options.Value.MaxConcurrentConnections);
    }

    public Task<bool> AddConnectionAsync(WebSocket socket, CancellationToken cancellationToken = default)
    {
        if (_subscriptions.Count >= _maxConcurrentConnections)
        {
            return Task.FromResult(false);
        }

        var added = _subscriptions.TryAdd(socket, null);
        return Task.FromResult(added);
    }

    public Task RemoveConnectionAsync(WebSocket socket, CancellationToken cancellationToken = default)
    {
        _subscriptions.TryRemove(socket, out _);
        return Task.CompletedTask;
    }

    public Task SubscribeAsync(WebSocket socket, string channel, CancellationToken cancellationToken = default)
    {
        _subscriptions.AddOrUpdate(socket, _ => channel, (_, _) => channel);
        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync(WebSocket socket, string channel, CancellationToken cancellationToken = default)
    {
        _subscriptions.AddOrUpdate(socket, _ => (string?)null, (_, currentChannel) =>
            string.Equals(currentChannel, channel, StringComparison.Ordinal) ? null : currentChannel);

        return Task.CompletedTask;
    }

    public IReadOnlyList<string> GetSubscribedChannels()
    {
        return _subscriptions.Values
            .Where(channel => string.IsNullOrWhiteSpace(channel) == false)
            .Distinct(StringComparer.Ordinal)
            .ToList()!;
    }

    public IReadOnlyList<WebSocket> GetSubscribers(string channel)
    {
        return _subscriptions
            .Where(entry =>
                entry.Key.State == WebSocketState.Open &&
                string.Equals(entry.Value, channel, StringComparison.Ordinal))
            .Select(entry => entry.Key)
            .ToList();
    }

    public int GetConnectionCount() => _subscriptions.Count;
}
