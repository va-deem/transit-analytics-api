using Microsoft.AspNetCore.Mvc;
using TransitAnalyticsAPI.Admin.Services;

namespace TransitAnalyticsAPI.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly IAdminSettingsService _adminSettingsService;

    public HealthController(IAdminSettingsService adminSettingsService)
    {
        _adminSettingsService = adminSettingsService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var isMaintenanceMode = await _adminSettingsService.IsMaintenanceModeEnabledAsync(cancellationToken);

        return Ok(new
        {
            status = "ok",
            maintenanceMode = isMaintenanceMode
        });
    }
}
