namespace TransitAnalyticsAPI.Admin.Services;

public interface IPollingRuntimeState
{
    bool IsPollingEnabled { get; }

    bool IsMaintenanceModeEnabled { get; }

    void SetPollingEnabled(bool isEnabled);

    void SetMaintenanceModeEnabled(bool isEnabled);

    Task WaitUntilPollingEnabledAsync(CancellationToken cancellationToken);

    Task WaitForPollingStateChangeOrTimeoutAsync(TimeSpan timeout, CancellationToken cancellationToken);
}
