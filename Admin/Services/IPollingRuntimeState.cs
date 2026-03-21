namespace TransitAnalyticsAPI.Admin.Services;

public interface IPollingRuntimeState
{
    bool IsPollingEnabled { get; }

    void SetPollingEnabled(bool isEnabled);

    Task WaitUntilPollingEnabledAsync(CancellationToken cancellationToken);

    Task WaitForPollingStateChangeOrTimeoutAsync(TimeSpan timeout, CancellationToken cancellationToken);
}
