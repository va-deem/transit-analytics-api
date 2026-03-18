namespace TransitAnalyticsAPI.Models.Dto;

public class RouteShapePointDto
{
    public string ShapeId { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public int Sequence { get; set; }
}
