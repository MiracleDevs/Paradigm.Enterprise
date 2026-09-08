using System.Diagnostics;

namespace BeaconAr.DatabaseBootstrap;

internal static class ProcessRunner
{
    #region Public Methods

    public static async Task RunAsync(
        string workingDirectory,
        TimeSpan timeout,
        string fileName,
        IReadOnlyCollection<string> arguments,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        using var deadline = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
            },
        };
        foreach (var argument in arguments)
            process.StartInfo.ArgumentList.Add(argument);
        if (environment is not null)
        {
            foreach (var pair in environment)
                process.StartInfo.Environment[pair.Key] = pair.Value;
        }

        process.Start();
        try
        {
            await process.WaitForExitAsync(linked.Token);
        }
        catch (OperationCanceledException)
        {
            await KillAndReapAsync(process);
            throw;
        }

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Process '{fileName}' failed with exit code {process.ExitCode}.");
    }

    public static async Task<string> RunAndCaptureAsync(
        string workingDirectory,
        TimeSpan timeout,
        string fileName,
        IReadOnlyCollection<string> arguments,
        IReadOnlySet<int> acceptedExitCodes,
        CancellationToken cancellationToken)
    {
        using var deadline = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            },
        };
        foreach (var argument in arguments)
            process.StartInfo.ArgumentList.Add(argument);

        process.Start();
        var standardOutput = process.StandardOutput.ReadToEndAsync(linked.Token);
        var standardError = process.StandardError.ReadToEndAsync(linked.Token);
        try
        {
            await process.WaitForExitAsync(linked.Token);
            await Task.WhenAll(standardOutput, standardError);
        }
        catch (OperationCanceledException)
        {
            await KillAndReapAsync(process);
            throw;
        }

        if (!acceptedExitCodes.Contains(process.ExitCode))
            throw new InvalidOperationException($"Process '{fileName}' failed with exit code {process.ExitCode}.");

        return string.Concat(await standardOutput, Environment.NewLine, await standardError);
    }

    #endregion

    #region Private Methods

    private static async Task KillAndReapAsync(Process process)
    {
        if (!process.HasExited)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }
        }

        await process.WaitForExitAsync(CancellationToken.None);
    }

    #endregion
}
