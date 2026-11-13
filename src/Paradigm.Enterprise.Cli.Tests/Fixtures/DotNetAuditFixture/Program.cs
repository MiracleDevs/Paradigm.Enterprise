using System.Diagnostics;

if (args.Contains("--hold-output", StringComparer.Ordinal))
{
    await Task.Delay(TimeSpan.FromSeconds(5));
    return 0;
}

if (args.Length > 0 && args[0] == "msbuild")
{
    if (!args.Contains("-nodeReuse:false", StringComparer.Ordinal) ||
        !args.Contains("-maxCpuCount:1", StringComparer.Ordinal) ||
        !args.Contains("-property:UseSharedCompilation=false", StringComparer.Ordinal) ||
        Environment.GetEnvironmentVariable("MSBUILDDISABLENODEREUSE") != "1" ||
        Environment.GetEnvironmentVariable("DOTNET_CLI_USE_MSBUILD_SERVER") != "0")
        return 3;

    await Console.Out.WriteLineAsync("evaluation output");
    await Console.Error.WriteLineAsync("evaluation diagnostic");
    if (args.Contains("ProcessFailure.csproj", StringComparer.Ordinal))
        return 7;
    if (args.Contains("ProcessTimeout.csproj", StringComparer.Ordinal))
        await Task.Delay(Timeout.InfiniteTimeSpan);
    if (args.Contains("ProcessInheritedOutput.csproj", StringComparer.Ordinal))
    {
        // The parent exits, but its child keeps the redirected pipes open.
        using var child = Process.Start(new ProcessStartInfo
        {
            FileName = Environment.ProcessPath!,
            UseShellExecute = false,
            ArgumentList = { "--hold-output" }
        });
    }

    return 0;
}

var projectIndex = Array.IndexOf(args, "--project");
var project = projectIndex >= 0 && projectIndex + 1 < args.Length
    ? args[projectIndex + 1]
    : "";
if (Path.GetFileName(project).Equals("AuditFailure.csproj", StringComparison.OrdinalIgnoreCase))
{
    await Console.Error.WriteAsync("private feed unavailable");
    return 9;
}

if (Path.GetFileName(project).Equals("AuditMalformed.csproj", StringComparison.OrdinalIgnoreCase))
{
    await Console.Out.WriteAsync("{malformed");
    return 0;
}

if (args.Length >= 2 &&
    args[0] == "package" &&
    args[1] == "list" &&
    args.Contains("--project", StringComparer.Ordinal) &&
    args.Any(argument => argument is "--vulnerable" or "--deprecated" or "--outdated"))
{
    await Console.Out.WriteAsync("""{"version":1,"projects":[]}""");
    return 0;
}

await Console.Error.WriteAsync($"Unexpected deterministic dotnet-audit invocation: {string.Join(' ', args)}");
return 2;
