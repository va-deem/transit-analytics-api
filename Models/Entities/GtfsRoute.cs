namespace TransitAnalyticsAPI.Models.Entities;

public class GtfsRoute
{
    public long Id { get; set; }

    public long ImportRunId { get; set; }

    public string RouteId { get; set; } = string.Empty;

    public string? AgencyId { get; set; }

    public string? RouteShortName { get; set; }

    public string? RouteLongName { get; set; }

    public int? RouteType { get; set; }

    public string? RouteColor { get; set; }

    public string? RouteTextColor { get; set; }

    public GtfsImportRun? ImportRun { get; set; }
}
