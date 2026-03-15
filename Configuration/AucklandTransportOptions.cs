namespace TransitAnalyticsAPI.Configuration;

public class AucklandTransportOptions
{
    public const string SectionName = "AucklandTransport";

    public string BaseUrl { get; set; } = "https://api.at.govt.nz";

    public string SubscriptionKey { get; set; } = string.Empty;
}
