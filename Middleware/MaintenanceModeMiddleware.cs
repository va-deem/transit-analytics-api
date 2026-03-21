using TransitAnalyticsAPI.Admin.Services;

namespace TransitAnalyticsAPI.Middleware;

public class MaintenanceModeMiddleware
{
    private readonly RequestDelegate _next;

    public MaintenanceModeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAdminSettingsService adminSettingsService)
    {
        if (!await adminSettingsService.IsMaintenanceModeEnabledAsync(context.RequestAborted))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path;
        if (path.StartsWithSegments("/admin") || path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "service_unavailable",
            message = "The service is in maintenance mode."
        });
    }
}
