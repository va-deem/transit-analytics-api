using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransitAnalyticsAPI.Controllers;
using TransitAnalyticsAPI.Models.Dto;
using TransitAnalyticsAPI.Persistence;
using TransitAnalyticsAPI.Tests.TestHelpers;
using Xunit;

namespace TransitAnalyticsAPI.Tests.Controllers;

public class FeedbackControllerTests
{
    [Fact]
    public async Task Create_PersistsSubmissionAndReturnsCreated()
    {
        await using var dbContext = TestDbContextFactory.CreateSqliteDbContext();
        var now = new DateTimeOffset(2026, 5, 26, 9, 35, 0, TimeSpan.Zero);
        var controller = CreateController(dbContext, new FakeTimeProvider(now), remoteIpAddress: "203.0.113.10");

        var result = await controller.Create(new FeedbackSubmissionRequestDto
        {
            Name = "  Jane Doe  ",
            Email = "  jane@example.com  ",
            Message = "  Love the app.  "
        }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(FeedbackController.GetById), created.ActionName);

        var submission = await dbContext.FeedbackSubmissions.SingleAsync();
        Assert.Equal("Jane Doe", submission.Name);
        Assert.Equal("jane@example.com", submission.Email);
        Assert.Equal("Love the app.", submission.Message);
        Assert.Equal("203.0.113.10", submission.Ip);
        Assert.Equal(now.UtcDateTime, submission.CreatedAtUtc);
    }

    [Fact]
    public async Task Create_UsesRequestIp_WhenProvided()
    {
        await using var dbContext = TestDbContextFactory.CreateSqliteDbContext();
        var controller = CreateController(dbContext, TimeProvider.System, remoteIpAddress: "203.0.113.10");

        await controller.Create(new FeedbackSubmissionRequestDto
        {
            Name = "Jane Doe",
            Email = "jane@example.com",
            Message = "Love the app.",
            Ip = "198.51.100.20"
        }, CancellationToken.None);

        var submission = await dbContext.FeedbackSubmissions.SingleAsync();
        Assert.Equal("198.51.100.20", submission.Ip);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenMessageIsBlank()
    {
        await using var dbContext = TestDbContextFactory.CreateSqliteDbContext();
        var controller = CreateController(dbContext, TimeProvider.System);

        var result = await controller.Create(new FeedbackSubmissionRequestDto
        {
            Name = "Jane Doe",
            Email = "jane@example.com",
            Message = "   "
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await dbContext.FeedbackSubmissions.ToListAsync());
    }

    private static FeedbackController CreateController(
        AppDbContext dbContext,
        TimeProvider timeProvider,
        string? remoteIpAddress = null)
    {
        var controller = new FeedbackController(dbContext, timeProvider);
        var httpContext = new DefaultHttpContext();

        if (!string.IsNullOrWhiteSpace(remoteIpAddress))
        {
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse(remoteIpAddress);
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }
}
