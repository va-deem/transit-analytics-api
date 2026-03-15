using System.Text.Json.Serialization;

namespace TransitAnalyticsAPI.Clients.AucklandTransport.Models;

public class AucklandTransportTripDescriptor
{
    [JsonPropertyName("trip_id")]
    public string? TripId { get; set; }

    [JsonPropertyName("route_id")]
    public string? RouteId { get; set; }
}
