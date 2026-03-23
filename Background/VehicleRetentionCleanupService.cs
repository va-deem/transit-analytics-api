using TransitAnalyticsAPI.Services;

namespace TransitAnalyticsAPI.Background;

public class VehicleRetentionCleanupService : BackgroundService
{
    private static readonly TimeOnly CleanupTime = new(3, 0);
    private static readonly TimeZoneInfo AucklandTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific/Auckland");

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<VehicleRetentionCleanupService> _logger;

    public VehicleRetentionCleanupService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<VehicleRetentionCleanupService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var delay = GetDelayUntilNextRun(DateTimeOffset.UtcNow);
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                await RunCleanupAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host shutdown should stop the worker quietly.
        }
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var retentionService = scope.ServiceProvider.GetRequiredService<IVehicleRetentionService>();

            var deletedCount = await retentionService.DeleteExpiredAsync(cancellationToken);

            _logger.LogInformation(
                "Vehicle retention cleanup completed. Deleted {DeletedCount} expired vehicle positions.",
                deletedCount);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Vehicle retention cleanup failed.");
        }
    }

    private static TimeSpan GetDelayUntilNextRun(DateTimeOffset utcNow)
    {
        var localNow = TimeZoneInfo.ConvertTime(utcNow, AucklandTimeZone);
        var nextRunDate = localNow.TimeOfDay < CleanupTime.ToTimeSpan()
            ? DateOnly.FromDateTime(localNow.DateTime)
            : DateOnly.FromDateTime(localNow.DateTime).AddDays(1);
        var nextRunLocal = nextRunDate.ToDateTime(CleanupTime, DateTimeKind.Unspecified);
        var nextRunOffset = new DateTimeOffset(
            nextRunLocal,
            AucklandTimeZone.GetUtcOffset(nextRunLocal));

        return nextRunOffset - utcNow;
    }
}
