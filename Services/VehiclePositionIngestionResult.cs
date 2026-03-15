namespace TransitAnalyticsAPI.Services;

public class VehiclePositionIngestionResult
{
    public string Status { get; set; } = string.Empty;

    public double? HeaderTimestamp { get; set; }

    public int TotalEntities { get; set; }

    public int SavedVehiclePositions { get; set; }
}
