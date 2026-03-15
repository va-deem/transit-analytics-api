using System.Text.Json.Serialization;

namespace TransitAnalyticsAPI.Clients.AucklandTransport.Models;

public class AucklandTransportVehicleDescriptor
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("license_plate")]
    public string? LicensePlate { get; set; }
}
