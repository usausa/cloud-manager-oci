namespace CloudManager.Infrastructure.OracleCloud;

// Polls a resource state until it reaches the target, reporting progress against the timeout
public static class OciPolling
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    public static ValueTask WaitForStateAsync(
        Func<CancellationToken, Task<string>> getState,
        string targetState,
        int timeoutSeconds,
        IProgress<ProgressUpdate> progress,
        CancellationToken cancellationToken) =>
        WaitAsync(getState, state => String.Equals(state, targetState, StringComparison.OrdinalIgnoreCase), targetState, timeoutSeconds, progress, cancellationToken);

    public static async ValueTask WaitAsync(
        Func<CancellationToken, Task<string>> getState,
        Func<string, bool> isDone,
        string description,
        int timeoutSeconds,
        IProgress<ProgressUpdate> progress,
        CancellationToken cancellationToken)
    {
        var started = DateTime.UtcNow;
        var deadline = started.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(PollInterval, cancellationToken);

            var state = await getState(cancellationToken);
            var elapsed = (DateTime.UtcNow - started).TotalSeconds;
            progress.Report(new ProgressUpdate(Math.Min(elapsed / timeoutSeconds, 0.99), $"[{state}]"));

            if (isDone(state))
            {
                return;
            }
        }

        throw new TimeoutException($"Resource did not reach '{description}' within {timeoutSeconds} seconds.");
    }
}
