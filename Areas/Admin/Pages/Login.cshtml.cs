using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using TransitAnalyticsAPI.Admin.Security;
using TransitAnalyticsAPI.Configuration;

namespace TransitAnalyticsAPI.Areas.Admin.Pages;

public class LoginModel : PageModel
{
    private readonly IOptions<AdminOptions> _adminOptions;
    private readonly IAdminPasswordService _adminPasswordService;

    public LoginModel(IOptions<AdminOptions> adminOptions, IAdminPasswordService adminPasswordService)
    {
        _adminOptions = adminOptions;
        _adminPasswordService = adminPasswordService;
    }

    [BindProperty]
    [Required]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; private set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Settings");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            ErrorMessage = "Password is required.";
            return Page();
        }

        var options = _adminOptions.Value;
        if (!_adminPasswordService.Verify(Password, options.PasswordHash))
        {
            ErrorMessage = "Invalid password.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "admin"),
            new(ClaimTypes.Role, "admin")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true
            });

        return RedirectToPage("/Settings");
    }
}
