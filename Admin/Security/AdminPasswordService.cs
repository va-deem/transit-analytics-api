using Microsoft.AspNetCore.Identity;

namespace TransitAnalyticsAPI.Admin.Security;

public class AdminPasswordService : IAdminPasswordService
{
    private static readonly PasswordHasher<string> PasswordHasher = new();
    private const string AdminUserName = "admin";

    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        var result = PasswordHasher.VerifyHashedPassword(AdminUserName, passwordHash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
