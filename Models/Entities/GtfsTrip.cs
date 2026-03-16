namespace TransitAnalyticsAPI.Models.Entities;

public class GtfsTrip
{
    public long Id { get; set; }

    public long ImportRunId { get; set; }

    public string TripId { get; set; } = string.Empty;

    public string RouteId { get; set; } = string.Empty;

    public string? ServiceId { get; set; }

    public string? TripHeadsign { get; set; }

    public int? DirectionId { get; set; }

    public string? ShapeId { get; set; }

    public GtfsImportRun? ImportRun { get; set; }
}
