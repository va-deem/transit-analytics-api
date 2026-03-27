using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TransitAnalyticsAPI.Configuration;
using TransitAnalyticsAPI.Middleware;
using TransitAnalyticsAPI.Tests.TestHelpers;
using Xunit;

namespace TransitAnalyticsAPI.Tests.Middleware;

public class InternalApiSecretMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ReturnsForbidden_ForProtectedPathWithoutValidSecret()
    {
        var nextWasCalled = false;
        var middleware = new InternalApiSecretMiddleware(
            context =>
            {
                nextWasCalled = true;
                return Task.CompletedTask;
            },
            Options.Create(new InternalApiOptions
            {
                Secret = "expected-secret",
                HeaderName = "X-Internal-Secret"
            }),
            new TestWebHostEnvironment { EnvironmentName = "Production" });

        var context = new DefaultHttpContext();
        context.Request.Path = "/vehicles/latest";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.False(nextWasCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/admin/login")]
    [InlineData("/ws/vehicles")]
    public async Task InvokeAsync_AllowsExcludedPaths_WithoutSecret(string path)
    {
        var nextWasCalled = false;
        var middleware = new InternalApiSecretMiddleware(
            context =>
            {
                nextWasCalled = true;
                return Task.CompletedTask;
            },
            Options.Create(new InternalApiOptions
            {
                Secret = "expected-secret",
                HeaderName = "X-Internal-Secret"
            }),
            new TestWebHostEnvironment { EnvironmentName = "Production" });

        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.True(nextWasCalled);
    }
}
