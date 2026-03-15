using System.Text.Json.Serialization;

namespace TransitAnalyticsAPI.Clients.AucklandTransport.Models;

public class AucklandTransportVehicleWrapper
{
    [JsonPropertyName("trip")]
    public AucklandTransportTripDescriptor? Trip { get; set; }

    [JsonPropertyName("vehicle")]
    public AucklandTransportVehicleDescriptor? Vehicle { get; set; }

    [JsonPropertyName("position")]
    public AucklandTransportPosition? Position { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
}
