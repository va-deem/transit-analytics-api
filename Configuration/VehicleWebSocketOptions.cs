namespace TransitAnalyticsAPI.Configuration;

public class VehicleWebSocketOptions
{
    public const string SectionName = "VehicleWebSocket";

    public int MaxConcurrentConnections { get; set; } = 250;

    public int MaxConnectionsPerIp { get; set; } = 10;

    public int MaxMessagesPerMinute { get; set; } = 60;

    public int MaxMessageSizeBytes { get; set; } = 4 * 1024;

    public string[] AllowedOrigins { get; set; } = [];
}
