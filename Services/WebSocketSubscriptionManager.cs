using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace TransitAnalyticsAPI.Services;

public class WebSocketSubscriptionManager : IWebSocketSubscriptionManager
{
    private readonly ConcurrentDictionary<WebSocket, ConnectionInfo> _connections = new();
    private readonly ConcurrentDictionary<string, int> _connectionCountsByIp = new();
    private readonly int _maxConcurrentConnections;
    private readonly int _maxConnectionsPerIp;

    public WebSocketSubscriptionManager(Microsoft.Extensions.Options.IOptions<TransitAnalyticsAPI.Configuration.VehicleWebSocketOptions> options)
    {
        _maxConcurrentConnections = Math.Max(1, options.Value.MaxConcurrentConnections);
        _maxConnectionsPerIp = Math.Max(1, options.Value.MaxConnectionsPerIp);
    }

    public Task<bool> AddConnectionAsync(WebSocket socket, string ipAddress, CancellationToken cancellationToken = default)
    {
        if (_connections.Count >= _maxConcurrentConnections)
        {
            return Task.FromResult(false);
        }

        var ipCount = _connectionCountsByIp.GetValueOrDefault(ipAddress, 0);
        if (ipCount >= _maxConnectionsPerIp)
        {
            return Task.FromResult(false);
        }

        if (!_connections.TryAdd(socket, new ConnectionInfo(ipAddress)))
        {
            return Task.FromResult(false);
        }

        _connectionCountsByIp.AddOrUpdate(ipAddress, 1, (_, count) => count + 1);
        return Task.FromResult(true);
    }

    public Task RemoveConnectionAsync(WebSocket socket, CancellationToken cancellationToken = default)
    {
        if (_connections.TryRemove(socket, out var info))
        {
            _connectionCountsByIp.AddOrUpdate(info.IpAddress, 0, (_, count) => Math.Max(0, count - 1));
        }

        return Task.CompletedTask;
    }

    public Task SubscribeAsync(WebSocket socket, string channel, CancellationToken cancellationToken = default)
    {
        if (_connections.TryGetValue(socket, out var info))
        {
            info.Channel = channel;
        }

        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync(WebSocket socket, string channel, CancellationToken cancellationToken = default)
    {
        if (_connections.TryGetValue(socket, out var info) &&
            string.Equals(info.Channel, channel, StringComparison.Ordinal))
        {
            info.Channel = null;
        }

        return Task.CompletedTask;
    }

    public IReadOnlyList<string> GetSubscribedChannels()
    {
        return _connections.Values
            .Select(info => info.Channel)
            .Where(channel => !string.IsNullOrWhiteSpace(channel))
            .Distinct(StringComparer.Ordinal)
            .ToList()!;
    }

    public IReadOnlyList<WebSocket> GetSubscribers(string channel)
    {
        return _connections
            .Where(entry =>
                entry.Key.State == WebSocketState.Open &&
                string.Equals(entry.Value.Channel, channel, StringComparison.Ordinal))
            .Select(entry => entry.Key)
            .ToList();
    }

    public int GetConnectionCount() => _connections.Count;

    private sealed class ConnectionInfo(string ipAddress)
    {
        public string IpAddress { get; } = ipAddress;
        public string? Channel { get; set; }
    }
}
