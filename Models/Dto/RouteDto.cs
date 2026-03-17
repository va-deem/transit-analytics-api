namespace TransitAnalyticsAPI.Models.Dto;

public class RouteDto
{
    public string RouteId { get; set; } = string.Empty;

    public string? RouteShortName { get; set; }

    public string? RouteLongName { get; set; }

    public int? RouteType { get; set; }

    public string? VehicleType { get; set; }

    public string? RouteColor { get; set; }

    public int LatestVehicleCount { get; set; }
}
