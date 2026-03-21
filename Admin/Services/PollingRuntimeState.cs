namespace TransitAnalyticsAPI.Admin.Services;

public class PollingRuntimeState : IPollingRuntimeState
{
    private readonly object _sync = new();
    private TaskCompletionSource<bool> _stateChanged = CreateStateChangedSource();

    public bool IsPollingEnabled { get; private set; }

    public void SetPollingEnabled(bool isEnabled)
    {
        lock (_sync)
        {
            if (IsPollingEnabled == isEnabled)
            {
                return;
            }

            IsPollingEnabled = isEnabled;
            var previous = _stateChanged;
            _stateChanged = CreateStateChangedSource();
            previous.TrySetResult(true);
        }
    }

    public async Task WaitUntilPollingEnabledAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            Task stateChangedTask;

            lock (_sync)
            {
                if (IsPollingEnabled)
                {
                    return;
                }

                stateChangedTask = _stateChanged.Task;
            }

            await stateChangedTask.WaitAsync(cancellationToken);
        }
    }

    public async Task WaitForPollingStateChangeOrTimeoutAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        Task stateChangedTask;

        lock (_sync)
        {
            stateChangedTask = _stateChanged.Task;
        }

        var delayTask = Task.Delay(timeout, cancellationToken);
        await Task.WhenAny(delayTask, stateChangedTask);
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static TaskCompletionSource<bool> CreateStateChangedSource()
    {
        return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
