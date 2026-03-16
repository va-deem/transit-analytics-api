namespace TransitAnalyticsAPI.Models.Entities;

public class GtfsImportRun
{
    public long Id { get; set; }

    public string SourceVersion { get; set; } = string.Empty;

    public DateTime StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public List<GtfsRoute> Routes { get; set; } = [];

    public List<GtfsTrip> Trips { get; set; } = [];
}
