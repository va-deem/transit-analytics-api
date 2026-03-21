using TransitAnalyticsAPI.Services;
using TransitAnalyticsAPI.Admin.Services;

namespace TransitAnalyticsAPI.Background;

public class VehiclePollingService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IPollingRuntimeState _pollingRuntimeState;
    private readonly ILogger<VehiclePollingService> _logger;

    public VehiclePollingService(
        IServiceScopeFactory serviceScopeFactory,
        IPollingRuntimeState pollingRuntimeState,
        ILogger<VehiclePollingService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _pollingRuntimeState = pollingRuntimeState;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _pollingRuntimeState.WaitUntilPollingEnabledAsync(stoppingToken);
                await RunIngestionAsync(stoppingToken);

                try
                {
                    await _pollingRuntimeState.WaitForPollingStateChangeOrTimeoutAsync(PollInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host shutdown should stop the worker quietly.
        }
    }

    private async Task RunIngestionAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var ingestionService = scope.ServiceProvider.GetRequiredService<IVehiclePositionIngestionService>();
            var broadcastService = scope.ServiceProvider.GetRequiredService<IVehicleSnapshotBroadcastService>();

            var result = await ingestionService.IngestAsync(cancellationToken);
            await broadcastService.BroadcastAsync(cancellationToken);

            _logger.LogInformation(
                "Vehicle polling completed. Status: {Status}. Entities: {TotalEntities}. Saved: {SavedVehiclePositions}.",
                result.Status,
                result.TotalEntities,
                result.SavedVehiclePositions);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Vehicle polling failed.");
        }
    }
}
