namespace TransitAnalyticsAPI.Admin.Services;

public class AdminSettingsBootstrapService : IHostedService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IPollingRuntimeState _pollingRuntimeState;
    private readonly ILogger<AdminSettingsBootstrapService> _logger;

    public AdminSettingsBootstrapService(
        IServiceScopeFactory serviceScopeFactory,
        IPollingRuntimeState pollingRuntimeState,
        ILogger<AdminSettingsBootstrapService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _pollingRuntimeState = pollingRuntimeState;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var adminSettingsService = scope.ServiceProvider.GetRequiredService<IAdminSettingsService>();
        var settings = await adminSettingsService.GetAsync(cancellationToken);
        _pollingRuntimeState.SetPollingEnabled(settings.IsPollingEnabled);
        var startupMessage =
            $"Admin settings initialized.\nmaintenance: db={settings.IsMaintenanceMode}, memory=n/a\npolling:     db={settings.IsPollingEnabled}, memory={_pollingRuntimeState.IsPollingEnabled}";

        WriteHighlightedMessage(startupMessage);

        _logger.LogInformation(
            "{StartupMessage}",
            startupMessage);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static void WriteHighlightedMessage(string message)
    {
        var originalForeground = Console.ForegroundColor;

        try
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
        }
        finally
        {
            Console.ForegroundColor = originalForeground;
        }
    }
}
