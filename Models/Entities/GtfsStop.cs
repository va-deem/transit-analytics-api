namespace TransitAnalyticsAPI.Models.Entities;

public class GtfsStop
{
    public long Id { get; set; }

    public long ImportRunId { get; set; }

    public string StopId { get; set; } = string.Empty;

    public string? StopCode { get; set; }

    public string? StopName { get; set; }

    public double StopLat { get; set; }

    public double StopLon { get; set; }

    public int? LocationType { get; set; }

    public string? ParentStation { get; set; }

    public string? PlatformCode { get; set; }

    public GtfsImportRun? ImportRun { get; set; }
}
