namespace TransitAnalyticsAPI.Configuration;

public class InternalApiOptions
{
    public const string SectionName = "InternalApi";

    public string Secret { get; set; } = string.Empty;

    public string HeaderName { get; set; } = "X-Internal-Secret";
}
