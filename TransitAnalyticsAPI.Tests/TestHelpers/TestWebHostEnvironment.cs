using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace TransitAnalyticsAPI.Tests.TestHelpers;

internal sealed class TestWebHostEnvironment : IWebHostEnvironment
{
    public string EnvironmentName { get; set; } = string.Empty;

    public string ApplicationName { get; set; } = "TransitAnalyticsAPI.Tests";

    public string WebRootPath { get; set; } = string.Empty;

    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

    public string ContentRootPath { get; set; } = string.Empty;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
