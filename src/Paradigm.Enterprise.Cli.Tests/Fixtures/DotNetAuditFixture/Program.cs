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
