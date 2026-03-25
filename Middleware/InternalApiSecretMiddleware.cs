using Microsoft.Extensions.Options;
using TransitAnalyticsAPI.Configuration;

namespace TransitAnalyticsAPI.Middleware;

public class InternalApiSecretMiddleware
{
    private static readonly PathString[] ExcludedPaths =
    [
        new("/admin"),
        new("/health"),
        new("/ws/vehicles")
    ];

    private readonly RequestDelegate _next;
    private readonly InternalApiOptions _options;
    private readonly IWebHostEnvironment _environment;

    public InternalApiSecretMiddleware(
        RequestDelegate next,
        IOptions<InternalApiOptions> options,
        IWebHostEnvironment environment)
    {
        _next = next;
        _options = options.Value;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_environment.IsDevelopment() || IsExcludedPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.Secret))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "internal_api_secret_not_configured",
                message = "The internal API secret is not configured."
            });
            return;
        }

        if (!context.Request.Headers.TryGetValue(_options.HeaderName, out var suppliedSecret) ||
            !string.Equals(suppliedSecret, _options.Secret, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "forbidden",
                message = "A valid internal API secret is required."
            });
            return;
        }

        await _next(context);
    }

    private static bool IsExcludedPath(PathString path)
    {
        foreach (var excludedPath in ExcludedPaths)
        {
            if (path.StartsWithSegments(excludedPath))
            {
                return true;
            }
        }

        return false;
    }
}
