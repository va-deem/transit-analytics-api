namespace TransitAnalyticsAPI.Configuration;

public class AdminOptions
{
    public const string SectionName = "Admin";

    public string PasswordHash { get; set; } = string.Empty;

    public string CookieName { get; set; } = "transit_admin";

    public int LoginRateLimitPermitLimit { get; set; } = 5;

    public int LoginRateLimitWindowMinutes { get; set; } = 10;
}
