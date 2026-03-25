using System.Net.WebSockets;

namespace TransitAnalyticsAPI.Services;

public interface IVehicleWebSocketService
{
    Task HandleConnectionAsync(WebSocket socket, string ipAddress, CancellationToken cancellationToken = default);

    Task SendSnapshotAsync(WebSocket socket, string channel, CancellationToken cancellationToken = default);
}
