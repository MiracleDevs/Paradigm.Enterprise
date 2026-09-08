using System.Diagnostics;
using BeaconAr.DatabaseBootstrap;
using Microsoft.Extensions.Logging;

namespace BeaconAr.DatabaseBootstrap.Tests;

[TestClass]
public sealed class BootstrapInfrastructureTests
{
    #region Public Methods

    [TestMethod]
    public void SqlCmdInvocationPreservesTlsPolicyAndKeepsPasswordOutOfArguments()
    {
        const string password = "not-on-command-line";

        var invocation = SqlCmdRunner.CreateInvocation(
            $"Server=database.example;Initial Catalog=BeaconAr;User ID=deployer;Password={password};Encrypt=True;TrustServerCertificate=True",
            "PrePreDeployment.sql",
            TimeSpan.FromMinutes(3));

        CollectionAssert.Contains(invocation.Arguments.ToArray(), "-N");
        CollectionAssert.Contains(invocation.Arguments.ToArray(), "-C");
        Assert.IsFalse(invocation.Arguments.Any(argument => argument.Contains(password, StringComparison.Ordinal)));
        Assert.AreEqual(password, invocation.Environment["SQLCMDPASSWORD"]);
    }

    [TestMethod]
    public void SqlCmdInvocationDoesNotAddCertificateTrustWhenItIsDisabled()
    {
        var invocation = SqlCmdRunner.CreateInvocation(
            "Server=database.example;Initial Catalog=BeaconAr;Integrated Security=True;Encrypt=True;TrustServerCertificate=False",
            "PrePreDeployment.sql",
            TimeSpan.FromMinutes(3));

        CollectionAssert.Contains(invocation.Arguments.ToArray(), "-N");
        CollectionAssert.DoesNotContain(invocation.Arguments.ToArray(), "-C");
        CollectionAssert.Contains(invocation.Arguments.ToArray(), "-E");
    }

    [TestMethod]
    public void SqlCmdInvocationRejectsTlsModesItCannotPreserve()
    {
        Assert.Throws<InvalidOperationException>(() => SqlCmdRunner.CreateInvocation(
            "Server=database.example;Initial Catalog=BeaconAr;Integrated Security=True;Encrypt=False",
            "PrePreDeployment.sql",
            TimeSpan.FromMinutes(3)));
        Assert.Throws<InvalidOperationException>(() => SqlCmdRunner.CreateInvocation(
            "Server=database.example;Initial Catalog=BeaconAr;Integrated Security=True;Encrypt=Strict",
            "PrePreDeployment.sql",
            TimeSpan.FromMinutes(3)));
    }

    [TestMethod]
    public async Task BootstrapPhaseEmitsStableStructuredCompletionAndFailureEvents()
    {
        var logger = new RecordingLogger();

        await BootstrapPhase.RunAsync(logger, "successful-phase", TimeSpan.FromSeconds(1), _ => Task.CompletedTask, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            BootstrapPhase.RunAsync(logger, "failed-phase", TimeSpan.FromSeconds(1), _ => throw new InvalidOperationException("sensitive detail"), CancellationToken.None));

        var completed = logger.Entries.Single(entry => entry.EventId == BootstrapPhase.CompletedEvent);
        Assert.AreEqual("successful-phase", completed.Properties["Phase"]);
        Assert.AreEqual("completed", completed.Properties["Outcome"]);
        Assert.IsTrue(completed.Properties.ContainsKey("DurationMs"));

        var failed = logger.Entries.Single(entry => entry.EventId == BootstrapPhase.FailedEvent);
        Assert.AreEqual("failed-phase", failed.Properties["Phase"]);
        Assert.AreEqual("failed", failed.Properties["Outcome"]);
        Assert.AreEqual(nameof(InvalidOperationException), failed.Properties["FailureKind"]);
        Assert.IsFalse(failed.Message.Contains("sensitive detail", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task CancelledProcessIsKilledAndReapedWithItsChildren()
    {
        var marker = Path.Combine(Path.GetTempPath(), $"beacon-ar-process-{Guid.NewGuid():N}.txt");
        try
        {
            var (fileName, arguments) = CreateDelayedMarkerCommand(marker);
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
            var stopwatch = Stopwatch.StartNew();

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                ProcessRunner.RunAsync(AppContext.BaseDirectory, TimeSpan.FromSeconds(10), fileName, arguments, cancellation.Token));

            stopwatch.Stop();
            Assert.IsLessThan(TimeSpan.FromSeconds(5), stopwatch.Elapsed);
            await Task.Delay(TimeSpan.FromSeconds(2));
            Assert.IsFalse(File.Exists(marker), "The descendant process survived cancellation and wrote its marker.");
        }
        finally
        {
            if (File.Exists(marker))
                File.Delete(marker);
        }
    }

    #endregion

    #region Private Methods

    private static (string FileName, string[] Arguments) CreateDelayedMarkerCommand(string marker)
    {
        if (OperatingSystem.IsWindows())
            return ("cmd.exe", ["/d", "/s", "/c", $"ping -n 3 127.0.0.1 >nul & echo survived>\"{marker}\""]);

        var escapedMarker = marker.Replace("'", "'\\''", StringComparison.Ordinal);
        return ("/bin/sh", ["-c", $"sleep 1; printf survived > '{escapedMarker}'"]);
    }

    private sealed class RecordingLogger : ILogger
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var properties = state is IEnumerable<KeyValuePair<string, object?>> values
                ? values.Where(value => value.Key != "{OriginalFormat}").ToDictionary(value => value.Key, value => value.Value)
                : new Dictionary<string, object?>();
            Entries.Add(new LogEntry(eventId, formatter(state, exception), properties));
        }
    }

    private sealed record LogEntry(EventId EventId, string Message, IReadOnlyDictionary<string, object?> Properties);

    #endregion
}
