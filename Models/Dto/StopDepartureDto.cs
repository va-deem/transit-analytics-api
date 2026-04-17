namespace TransitAnalyticsAPI.Models.Dto;

public class StopDepartureDto
{
    public string RouteShortName { get; set; } = string.Empty;

    public string? RouteLongName { get; set; }

    public string? TripHeadsign { get; set; }

    public DateTime ScheduledArrival { get; set; }

    public DateTime? EstimatedArrival { get; set; }

    public string? VehicleId { get; set; }

    public string? RouteColor { get; set; }
}
