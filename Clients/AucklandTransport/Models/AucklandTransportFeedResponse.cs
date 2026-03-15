using System.Text.Json.Serialization;

namespace TransitAnalyticsAPI.Clients.AucklandTransport.Models;

public class AucklandTransportFeedResponse
{
    [JsonPropertyName("header")]
    public AucklandTransportFeedHeader? Header { get; set; }

    [JsonPropertyName("entity")]
    public List<AucklandTransportFeedEntity> Entity { get; set; } = [];
}
