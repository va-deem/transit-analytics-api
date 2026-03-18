namespace TransitAnalyticsAPI.Models.Entities;

public class GtfsStopTime
{
    public long Id { get; set; }

    public long ImportRunId { get; set; }

    public string TripId { get; set; } = string.Empty;

    public string StopId { get; set; } = string.Empty;

    public int StopSequence { get; set; }

    public string? StopHeadsign { get; set; }

    public double? ShapeDistTraveled { get; set; }

    public GtfsImportRun? ImportRun { get; set; }
}
