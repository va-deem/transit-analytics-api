namespace TransitAnalyticsAPI.Configuration;

public class VehicleOptions
{
    public const string SectionName = "Vehicles";

    public int LatestPositionMaxAgeMinutes { get; set; } = 5;

    public int HistoryRetentionDays { get; set; } = 7;
}
