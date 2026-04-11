using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TransitAnalyticsAPI.Background;
using TransitAnalyticsAPI.Models.Entities;
using TransitAnalyticsAPI.Services;
using Xunit;

namespace TransitAnalyticsAPI.Tests.Background;

public class VehicleRetentionCleanupServiceTests
{
    private static readonly TimeZoneInfo AucklandTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Pacific/Auckland");

    [Theory]
    [InlineData(1, 0, 2, 0)]  // 1:00 AM NZST → next run at 3:00 AM same day = 2h
    [InlineData(3, 0, 24, 0)] // 3:00 AM NZST → next run at 3:00 AM next day = 24h
    [InlineData(4, 0, 23, 0)] // 4:00 AM NZST → next run at 3:00 AM next day = 23h
    [InlineData(23, 30, 3, 30)] // 11:30 PM NZST → next run at 3:00 AM next day = 3.5h
    public void GetDelayUntilNextRun_ReturnsCorrectDelay(
        int localHour, int localMinute, int expectedHours, int expectedMinutes)
    {
        var localDateTime = new DateTime(2026, 4, 10, localHour, localMinute, 0, DateTimeKind.Unspecified);
        var utcOffset = AucklandTimeZone.GetUtcOffset(localDateTime);
        var utcNow = new DateTimeOffset(localDateTime, utcOffset).ToUniversalTime();

        var method = typeof(VehicleRetentionCleanupService)
            .GetMethod("GetDelayUntilNextRun", BindingFlags.NonPublic | BindingFlags.Static)!;

        var result = (TimeSpan)method.Invoke(null, [utcNow])!;

        Assert.Equal(new TimeSpan(expectedHours, expectedMinutes, 0), result);
    }

    [Fact]
    public async Task RunCleanupAsync_OnSuccess_LogsInfoWithDeletedCount()
    {
        var retentionService = new FakeVehicleRetentionService(deletedCount: 42);
        var systemLog = new FakeSystemLogService();
        var sut = CreateService(retentionService, systemLog);

        await InvokeRunCleanupAsync(sut);

        var entry = Assert.Single(systemLog.Entries);
        Assert.Equal(SystemLogType.Info, entry.Type);
        Assert.Contains("42", entry.Details);
    }

    [Fact]
    public async Task RunCleanupAsync_OnFailure_LogsError()
    {
        var retentionService = new FakeVehicleRetentionService(exception: new InvalidOperationException("db error"));
        var systemLog = new FakeSystemLogService();
        var sut = CreateService(retentionService, systemLog);

        await InvokeRunCleanupAsync(sut);

        var entry = Assert.Single(systemLog.Entries);
        Assert.Equal(SystemLogType.Error, entry.Type);
        Assert.Contains("db error", entry.Details);
    }

    [Fact]
    public async Task RunCleanupAsync_OnCancellation_Throws()
    {
        var cts = new CancellationTokenSource();
        var retentionService = new FakeVehicleRetentionService(exception: new OperationCanceledException());
        var systemLog = new FakeSystemLogService();
        var sut = CreateService(retentionService, systemLog);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => InvokeRunCleanupAsync(sut, cts.Token));

        Assert.Empty(systemLog.Entries);
    }

    private static VehicleRetentionCleanupService CreateService(
        IVehicleRetentionService retentionService,
        ISystemLogService<VehicleRetentionCleanupService> systemLog)
    {
        var services = new ServiceCollection();
        services.AddSingleton(retentionService);
        services.AddSingleton(systemLog);
        var serviceProvider = services.BuildServiceProvider();

        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var logger = NullLogger<VehicleRetentionCleanupService>.Instance;

        return new VehicleRetentionCleanupService(scopeFactory, logger);
    }

    private static async Task InvokeRunCleanupAsync(
        VehicleRetentionCleanupService service, CancellationToken ct = default)
    {
        var method = typeof(VehicleRetentionCleanupService)
            .GetMethod("RunCleanupAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;

        await (Task)method.Invoke(service, [ct])!;
    }

    private sealed class FakeVehicleRetentionService : IVehicleRetentionService
    {
        private readonly int _deletedCount;
        private readonly Exception? _exception;

        public FakeVehicleRetentionService(int deletedCount = 0, Exception? exception = null)
        {
            _deletedCount = deletedCount;
            _exception = exception;
        }

        public Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default)
        {
            if (_exception is not null) throw _exception;
            return Task.FromResult(_deletedCount);
        }
    }

    private sealed class FakeSystemLogService : ISystemLogService<VehicleRetentionCleanupService>
    {
        public List<(SystemLogType Type, string Description, string? Details)> Entries { get; } = [];

        public Task LogAsync(SystemLogType type, string description, string? details = null,
            CancellationToken cancellationToken = default)
        {
            Entries.Add((type, description, details));
            return Task.CompletedTask;
        }
    }
}
