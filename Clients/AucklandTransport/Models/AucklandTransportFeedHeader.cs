using System.Text.Json.Serialization;

namespace TransitAnalyticsAPI.Clients.AucklandTransport.Models;

public class AucklandTransportFeedHeader
{
    [JsonPropertyName("timestamp")]
    public double Timestamp { get; set; }

    [JsonPropertyName("gtfs_realtime_version")]
    public string GtfsRealtimeVersion { get; set; } = string.Empty;

    [JsonPropertyName("incrementality")]
    public int Incrementality { get; set; }
}
