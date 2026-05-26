namespace TransitAnalyticsAPI.Configuration;

public class FeedbackOptions
{
    public const string SectionName = "Feedback";

    public int RateLimitPermitLimit { get; set; } = 5;

    public int RateLimitWindowMinutes { get; set; } = 10;
}
