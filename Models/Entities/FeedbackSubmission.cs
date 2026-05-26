namespace TransitAnalyticsAPI.Models.Entities;

public class FeedbackSubmission
{
    public long Id { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Ip { get; set; } = string.Empty;
}
