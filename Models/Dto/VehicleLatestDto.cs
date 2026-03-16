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

    public string? RouteShortName { get; set; }

    public string? RouteLongName { get; set; }

    public int? RouteType { get; set; }

    public string? VehicleType { get; set; }

    public string? RouteColor { get; set; }

    public string? TripHeadsign { get; set; }

    public int? DirectionId { get; set; }
}
