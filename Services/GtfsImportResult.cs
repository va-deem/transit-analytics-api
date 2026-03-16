namespace TransitAnalyticsAPI.Services;

public class GtfsImportResult
{
    public string SourceVersion { get; set; } = string.Empty;

    public int ImportedRoutes { get; set; }

    public int ImportedTrips { get; set; }

    public long ImportRunId { get; set; }
}
