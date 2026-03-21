namespace TransitAnalyticsAPI.Configuration;

public class AdminOptions
{
    public const string SectionName = "Admin";

    public string PasswordHash { get; set; } = string.Empty;

    public string CookieName { get; set; } = "transit_admin";
}
