namespace TransitAnalyticsAPI.Services;

public static class VehicleTypeMapper
{
    public static string? Map(int? routeType)
    {
        return routeType switch
        {
            0 => "tram",
            1 => "subway",
            2 => "train",
            3 => "bus",
            4 => "ferry",
            5 => "cable_tram",
            6 => "aerial_lift",
            7 => "funicular",
            11 => "trolleybus",
            12 => "monorail",
            _ => null
        };
    }
}
