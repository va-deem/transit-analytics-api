namespace TransitAnalyticsAPI.Models.Dto;

public class RouteStopDto
{
    public string StopId { get; set; } = string.Empty;

    public string? StopCode { get; set; }

    public string? StopName { get; set; }

    public double StopLat { get; set; }

    public double StopLon { get; set; }

    public int StopSequence { get; set; }

    public string? PlatformCode { get; set; }
}
