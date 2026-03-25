namespace TransitAnalyticsAPI.Configuration;

public class VehicleWebSocketOptions
{
    public const string SectionName = "VehicleWebSocket";

    public int MaxConcurrentConnections { get; set; } = 250;

    public string[] AllowedOrigins { get; set; } = [];
}
