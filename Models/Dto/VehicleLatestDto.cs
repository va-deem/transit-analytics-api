namespace TransitAnalyticsAPI.Models.Dto;

public class VehicleLatestDto
{
    public string VehicleId { get; set; } = string.Empty;

    public string? TripId { get; set; }

    public string? RouteId { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public double? Speed { get; set; }

    public DateTime RecordedAtUtc { get; set; }
}
