using System.Text.Json.Serialization;

namespace TransitAnalyticsAPI.Clients.AucklandTransport.Models;

public class AucklandTransportPosition
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("bearing")]
    public string? Bearing { get; set; }

    [JsonPropertyName("speed")]
    public double? Speed { get; set; }
}
