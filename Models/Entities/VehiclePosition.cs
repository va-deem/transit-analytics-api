namespace TransitAnalyticsAPI.Models.Entities;

public class VehiclePosition
{
    public long Id { get; set; }

    public string VehicleId { get; set; } = string.Empty;

    public string? TripId { get; set; }

    public string? RouteId { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public string? Bearing { get; set; }

    public double? Speed { get; set; }

    public DateTime RecordedAtUtc { get; set; }

    public DateTime IngestedAtUtc { get; set; }

    public string? SourceEntityId { get; set; }
}
