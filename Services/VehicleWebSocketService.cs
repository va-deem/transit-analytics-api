using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using TransitAnalyticsAPI.Models.Dto;

namespace TransitAnalyticsAPI.Services;

public class VehicleWebSocketService : IVehicleWebSocketService
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IVehicleLatestQueryService _vehicleLatestQueryService;
    private readonly IWebSocketSubscriptionManager _subscriptionManager;
    private readonly ILogger<VehicleWebSocketService> _logger;

    public VehicleWebSocketService(
        IVehicleLatestQueryService vehicleLatestQueryService,
        IWebSocketSubscriptionManager subscriptionManager,
        ILogger<VehicleWebSocketService> logger)
    {
        _vehicleLatestQueryService = vehicleLatestQueryService;
        _subscriptionManager = subscriptionManager;
        _logger = logger;
    }

    public async Task HandleConnectionAsync(WebSocket socket, CancellationToken cancellationToken = default)
    {
        var accepted = await _subscriptionManager.AddConnectionAsync(socket, cancellationToken);
        if (!accepted)
        {
            _logger.LogWarning("Rejected websocket connection because the connection cap was reached.");
            await socket.CloseAsync(
                WebSocketCloseStatus.PolicyViolation,
                "Connection limit reached.",
                cancellationToken);
            return;
        }

        _logger.LogInformation(
            "Accepted websocket connection. Active connections: {ConnectionCount}.",
            _subscriptionManager.GetConnectionCount());

        var buffer = new byte[8 * 1024];

        try
        {
            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                if (result.MessageType != WebSocketMessageType.Text)
                {
                    continue;
                }

                var message = await ReadMessageAsync(socket, result, buffer, cancellationToken);
                if (string.IsNullOrWhiteSpace(message))
                {
                    continue;
                }

                var request = JsonSerializer.Deserialize<VehicleSubscriptionRequestDto>(message, JsonSerializerOptions);
                if (request is null || string.IsNullOrWhiteSpace(request.Type) || string.IsNullOrWhiteSpace(request.Channel))
                {
                    _logger.LogDebug("Ignored malformed websocket message.");
                    continue;
                }

                if (string.Equals(request.Type, "subscribe", StringComparison.OrdinalIgnoreCase))
                {
                    await _subscriptionManager.SubscribeAsync(socket, request.Channel, cancellationToken);
                    _logger.LogInformation("Websocket subscribed to channel {Channel}.", request.Channel);
                    await SendSnapshotAsync(socket, request.Channel, cancellationToken);
                }
                else if (string.Equals(request.Type, "unsubscribe", StringComparison.OrdinalIgnoreCase))
                {
                    await _subscriptionManager.UnsubscribeAsync(socket, request.Channel, cancellationToken);
                    _logger.LogInformation("Websocket unsubscribed from channel {Channel}.", request.Channel);
                }
            }
        }
        finally
        {
            await _subscriptionManager.RemoveConnectionAsync(socket, cancellationToken);
            _logger.LogInformation(
                "Closed websocket connection. Active connections: {ConnectionCount}.",
                _subscriptionManager.GetConnectionCount());

            if (socket.State != WebSocketState.Closed && socket.State != WebSocketState.Aborted)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed.", cancellationToken);
            }
        }
    }

    public async Task SendSnapshotAsync(
        WebSocket socket,
        string channel,
        CancellationToken cancellationToken = default)
    {
        var data = await GetSnapshotDataAsync(channel, cancellationToken);

        var message = new VehicleSnapshotMessageDto
        {
            Channel = channel,
            Data = data
        };

        var json = JsonSerializer.Serialize(message, JsonSerializerOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        await socket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            endOfMessage: true,
            cancellationToken);
    }

    private async Task<IReadOnlyList<VehicleLatestDto>> GetSnapshotDataAsync(
        string channel,
        CancellationToken cancellationToken)
    {
        var latestVehicles = await _vehicleLatestQueryService.GetLatestAsync(cancellationToken);

        if (string.Equals(channel, "vehicles:all", StringComparison.Ordinal))
        {
            return latestVehicles;
        }

        const string routePrefix = "vehicles:route:";
        if (channel.StartsWith(routePrefix, StringComparison.Ordinal))
        {
            var routeId = channel[routePrefix.Length..];

            return latestVehicles
                .Where(vehicle => string.Equals(vehicle.RouteId, routeId, StringComparison.Ordinal))
                .ToList();
        }

        return [];
    }

    private static async Task<string> ReadMessageAsync(
        WebSocket socket,
        WebSocketReceiveResult firstResult,
        byte[] buffer,
        CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        stream.Write(buffer, 0, firstResult.Count);

        var result = firstResult;
        while (!result.EndOfMessage)
        {
            result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            stream.Write(buffer, 0, result.Count);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
