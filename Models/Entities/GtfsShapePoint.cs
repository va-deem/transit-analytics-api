namespace TransitAnalyticsAPI.Models.Entities;

public class GtfsShapePoint
{
    public long Id { get; set; }

    public long ImportRunId { get; set; }

    public string ShapeId { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public int Sequence { get; set; }

    public double? DistanceTraveled { get; set; }

    public GtfsImportRun? ImportRun { get; set; }
}
