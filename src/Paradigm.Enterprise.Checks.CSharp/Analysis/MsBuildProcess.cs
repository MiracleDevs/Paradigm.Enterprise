using System.Diagnostics;
using System.Text;

namespace Paradigm.Enterprise.Checks.CSharp;

internal static class MsBuildProcess
{
    #region Public Methods

    public static string Run(IReadOnlyList<string> arguments, string executable = "dotnet", TimeSpan? timeout = null)
    {
        var limit = timeout ?? TimeSpan.FromSeconds(60);
        var output = new StringBuilder();
        var error = new StringBuilder();
        using var process = new Process
        {
            StartInfo = new()
            {
                FileName = executable,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                CreateNoWindow = true,
                ArgumentList = { "msbuild", "-nologo", "-nodeReuse:false", "-maxCpuCount:1", "-property:UseSharedCompilation=false" }
            }
        };
        foreach (var argument in arguments)
            process.StartInfo.ArgumentList.Add(argument);
        process.StartInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        process.StartInfo.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0";
        process.OutputDataReceived += (_, args) => Append(output, args.Data);
        process.ErrorDataReceived += (_, args) => Append(error, args.Data);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        using var cancellation = new CancellationTokenSource(limit);
        try
        {
            // WaitForExitAsync also waits for redirected streams to close. Bound both
            // process execution and draining pipes inherited by a child process.
            process.WaitForExitAsync(cancellation.Token).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
            throw new TimeoutException($"MSBuild timed out after {limit.TotalSeconds:g} seconds: {string.Join(' ', process.StartInfo.ArgumentList)}{Environment.NewLine}stdout:{Environment.NewLine}{Snapshot(output)}stderr:{Environment.NewLine}{Snapshot(error)}");
        }

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"MSBuild exited with code {process.ExitCode}: {string.Join(' ', process.StartInfo.ArgumentList)}{Environment.NewLine}stdout:{Environment.NewLine}{Snapshot(output)}stderr:{Environment.NewLine}{Snapshot(error)}");
        return Snapshot(output);
    }

    #endregion

    #region Private Methods

    private static void Append(StringBuilder builder, string? line)
    {
        if (line is null)
            return;
        lock (builder)
            builder.AppendLine(line);
    }

    private static string Snapshot(StringBuilder builder)
    {
        lock (builder)
            return builder.ToString();
    }

    #endregion
}
