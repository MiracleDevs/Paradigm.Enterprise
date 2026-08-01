using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace BeaconAr.DatabaseBootstrap;

internal static class BootstrapPhase
{
    internal static readonly EventId StartedEvent = new(1000, "BootstrapPhaseStarted");
    internal static readonly EventId CompletedEvent = new(1001, "BootstrapPhaseCompleted");
    internal static readonly EventId FailedEvent = new(1002, "BootstrapPhaseFailed");
    internal static readonly EventId SkippedEvent = new(1003, "BootstrapPhaseSkipped");
    private static readonly Action<ILogger, string, double, Exception?> LogStarted = LoggerMessage.Define<string, double>(
        LogLevel.Information, StartedEvent, "Bootstrap phase {Phase} started with timeout {TimeoutSeconds} seconds.");
    private static readonly Action<ILogger, string, long, string, Exception?> LogCompleted = LoggerMessage.Define<string, long, string>(
        LogLevel.Information, CompletedEvent, "Bootstrap phase {Phase} finished after {DurationMs} ms with outcome {Outcome}.");
    private static readonly Action<ILogger, string, long, string, string, Exception?> LogFailed = LoggerMessage.Define<string, long, string, string>(
        LogLevel.Error, FailedEvent, "Bootstrap phase {Phase} finished after {DurationMs} ms with outcome {Outcome} and failure kind {FailureKind}.");
    private static readonly Action<ILogger, string, long, string, string, Exception?> LogWarning = LoggerMessage.Define<string, long, string, string>(
        LogLevel.Warning, FailedEvent, "Bootstrap phase {Phase} finished after {DurationMs} ms with outcome {Outcome} and failure kind {FailureKind}.");
    private static readonly Action<ILogger, string, long, string, string, Exception?> LogPhaseSkipped = LoggerMessage.Define<string, long, string, string>(
        LogLevel.Information, SkippedEvent, "Bootstrap phase {Phase} finished after {DurationMs} ms with outcome {Outcome} and reason {Reason}.");

    #region Public Methods

    public static async Task RunAsync(
        ILogger logger,
        string phase,
        TimeSpan timeout,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        using var deadline = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
        var stopwatch = Stopwatch.StartNew();
        LogStarted(logger, phase, timeout.TotalSeconds, null);
        try
        {
            await operation(linked.Token);
            LogCompleted(logger, phase, stopwatch.ElapsedMilliseconds, "completed", null);
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            LogFailed(logger, phase, stopwatch.ElapsedMilliseconds, "failed", "timeout", null);
            throw new TimeoutException($"Bootstrap phase '{phase}' exceeded its configured deadline.");
        }
        catch (OperationCanceledException)
        {
            LogWarning(logger, phase, stopwatch.ElapsedMilliseconds, "cancelled", "cancellation", null);
            throw;
        }
        catch (Exception exception)
        {
            LogFailed(logger, phase, stopwatch.ElapsedMilliseconds, "failed", exception.GetType().Name, null);
            throw;
        }
    }

    public static void LogSkipped(ILogger logger, string phase, string reason)
    {
        LogPhaseSkipped(logger, phase, 0, "skipped", reason, null);
    }

    #endregion
}
