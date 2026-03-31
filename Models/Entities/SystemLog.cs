namespace TransitAnalyticsAPI.Models.Entities;

public class SystemLog
{
    public long Id { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public SystemLogType Type { get; set; }

    public string Source { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? Details { get; set; }
}
