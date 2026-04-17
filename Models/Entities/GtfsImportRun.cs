namespace TransitAnalyticsAPI.Models.Entities;

public enum GtfsImportStatus
{
    Running,
    Completed,
    Failed
}

public class GtfsImportRun
{
    public long Id { get; set; }

    public string SourceVersion { get; set; } = string.Empty;

    public DateTime StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public GtfsImportStatus Status { get; set; } = GtfsImportStatus.Running;

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public List<GtfsRoute> Routes { get; set; } = [];

    public List<GtfsCalendar> Calendars { get; set; } = [];

    public List<GtfsCalendarDate> CalendarDates { get; set; } = [];

    public List<GtfsShapePoint> ShapePoints { get; set; } = [];

    public List<GtfsStop> Stops { get; set; } = [];

    public List<GtfsStopTime> StopTimes { get; set; } = [];

    public List<GtfsTrip> Trips { get; set; } = [];
}
