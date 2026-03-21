using System.Threading.Channels;

namespace TransitAnalyticsAPI.Admin.Services;

public class GtfsUploadBackgroundService : BackgroundService
{
    private readonly Channel<GtfsUploadJob> _channel;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<GtfsUploadBackgroundService> _logger;

    public GtfsUploadBackgroundService(
        Channel<GtfsUploadJob> channel,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<GtfsUploadBackgroundService> logger)
    {
        _channel = channel;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var uploadService = scope.ServiceProvider.GetRequiredService<IGtfsUploadService>();
                var adminSettingsService = scope.ServiceProvider.GetRequiredService<IAdminSettingsService>();

                var result = await uploadService.ImportFromWorkRootAsync(
                    job.WorkRoot,
                    (status, cancellationToken) => adminSettingsService.UpdateGtfsUploadStatusAsync(
                        job.FileName,
                        status,
                        cancellationToken: cancellationToken),
                    stoppingToken);

                await adminSettingsService.RecordGtfsUploadResultAsync(
                    job.FileName,
                    isSuccessful: true,
                    result.SourceVersion,
                    error: null,
                    stoppingToken);

                _logger.LogInformation(
                    "GTFS upload completed. File: {FileName}. Source version: {SourceVersion}.",
                    job.FileName,
                    result.SourceVersion);
            }
            catch (Exception exception)
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var adminSettingsService = scope.ServiceProvider.GetRequiredService<IAdminSettingsService>();
                    await adminSettingsService.RecordGtfsUploadResultAsync(
                        job.FileName,
                        isSuccessful: false,
                        sourceVersion: null,
                        error: exception.Message,
                        stoppingToken);
                }
                catch (Exception followUpException)
                {
                    _logger.LogError(followUpException, "Failed to record GTFS upload failure status.");
                }

                _logger.LogError(exception, "GTFS upload failed for file {FileName}.", job.FileName);
            }
        }
    }
}
