namespace TransitAnalyticsAPI.Admin.Security;

public interface IAdminPasswordService
{
    bool Verify(string password, string passwordHash);
}
