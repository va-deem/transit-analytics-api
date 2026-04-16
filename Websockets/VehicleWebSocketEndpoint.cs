using Microsoft.Extensions.Options;
using TransitAnalyticsAPI.Admin.Services;
using TransitAnalyticsAPI.Configuration;
using TransitAnalyticsAPI.Services;

namespace TransitAnalyticsAPI.Websockets;

public static class VehicleWebSocketEndpoint
{
    public static void MapVehicleWebSocket(this WebApplication app)
    {
        app.Map("/ws/vehicles", async context =>
        {
            var webSocketOptions = context.RequestServices
                .GetRequiredService<IOptions<VehicleWebSocketOptions>>()
                .Value;
            var logger = context.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("VehicleWebSocketEndpoint");

            var adminSettingsService = context.RequestServices.GetRequiredService<IAdminSettingsService>();
            if (await adminSettingsService.IsMaintenanceModeEnabledAsync(context.RequestAborted))
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "service_unavailable",
                    message = "The service is in maintenance mode."
                });
                return;
            }

            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            if (!IsAllowedOrigin(context, webSocketOptions))
            {
                logger.LogWarning(
                    "Rejected websocket request from unexpected origin {Origin}.",
                    context.Request.Headers.Origin.ToString());
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "forbidden",
                    message = "The request origin is not allowed."
                });
                return;
            }

            var socket = await context.WebSockets.AcceptWebSocketAsync();
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            using var scope = app.Services.CreateScope();
            var webSocketService = scope.ServiceProvider.GetRequiredService<IVehicleWebSocketService>();

            await webSocketService.HandleConnectionAsync(socket, ipAddress, context.RequestAborted);
        });
    }

    private static bool IsAllowedOrigin(HttpContext context, VehicleWebSocketOptions options)
    {
        if (options.AllowedOrigins.Length == 0)
        {
            return true;
        }

        if (!context.Request.Headers.TryGetValue("Origin", out var originValues))
        {
            return false;
        }

        var origin = originValues.ToString();
        return options.AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
    }
}
