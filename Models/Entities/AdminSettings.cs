namespace TransitAnalyticsAPI.Models.Entities;

public class AdminSettings
{
    public int Id { get; set; }

    public bool IsMaintenanceMode { get; set; }

    public bool IsPollingEnabled { get; set; }

    public DateTime? LastGtfsUploadAtUtc { get; set; }

    public string? LastGtfsUploadFileName { get; set; }

    public string? LastGtfsImportStatus { get; set; }

    public string? LastGtfsImportError { get; set; }

    public string? LastGtfsSourceVersion { get; set; }
}
