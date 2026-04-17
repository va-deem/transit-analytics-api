namespace TransitAnalyticsAPI.Models.Entities;

public class GtfsCalendar
{
    public long Id { get; set; }

    public long ImportRunId { get; set; }

    public string ServiceId { get; set; } = string.Empty;

    public bool Monday { get; set; }

    public bool Tuesday { get; set; }

    public bool Wednesday { get; set; }

    public bool Thursday { get; set; }

    public bool Friday { get; set; }

    public bool Saturday { get; set; }

    public bool Sunday { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public GtfsImportRun? ImportRun { get; set; }
}
