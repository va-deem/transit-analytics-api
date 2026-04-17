namespace TransitAnalyticsAPI.Models.Entities;

public class GtfsCalendarDate
{
    public long Id { get; set; }

    public long ImportRunId { get; set; }

    public string ServiceId { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    public int ExceptionType { get; set; }

    public GtfsImportRun? ImportRun { get; set; }
}
