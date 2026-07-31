using System.Text.Json;

if (args.Length > 0)
{
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
    if (Path.GetFileName(project).Equals("CheckPackFixture.csproj", StringComparison.OrdinalIgnoreCase))
    {
        await Console.Out.WriteAsync("""{"version":1,"projects":[]}""");
        return 0;
    }
    File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "audit-fixture.pid"),
        Environment.ProcessId.ToString());
    await Task.Delay(Timeout.InfiniteTimeSpan);
    return 0;
}

using var request = JsonDocument.Parse(await Console.In.ReadToEndAsync());
var packId = request.RootElement.GetProperty("packId").GetString()!;
var packVersion = request.RootElement.GetProperty("packVersion").GetString()!;
switch (packId)
{
    case "nonzero":
        await Console.Error.WriteAsync("fixture failure");
        return 7;
    case "malformed":
        await Console.Out.WriteAsync("{not-json");
        return 0;
    case "oversized":
        await Console.Out.WriteAsync(new string('x', 4096));
        return 0;
    case "protocol":
        await Write("9.0", packId, packVersion, []);
        return 0;
    case "version-mismatch":
        await Write("1.0", packId, "9.9.9", []);
        return 0;
    case "wrong-prefix":
        await Write("1.0", packId, packVersion,
            [new("OTHER001", "error", "outside owned prefix", "Wrong.cs(1,1)")]);
        return 0;
    case "duplicate":
        var duplicate = new FixtureDiagnostic("PX001", "warning", "duplicate", "Duplicate.cs(1,1)");
        await Write("1.0", packId, packVersion, [duplicate, duplicate]);
        return 0;
    case "oversized-pipe":
        File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "check-fixture.pid"),
            Environment.ProcessId.ToString());
        while (true)
        {
            await Console.Out.WriteAsync(new string('x', 4096));
            await Console.Out.FlushAsync();
        }
    case "actual-version":
        await Write("1.0", packId, "1.1.0+fixture", []);
        return 0;
    case "timeout":
    case "cancel":
        File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "check-fixture.pid"),
            Environment.ProcessId.ToString());
        await Task.Delay(Timeout.InfiniteTimeSpan);
        return 0;
    default:
        await Write("1.0", packId, packVersion,
        [
            new("PX002", "warning", "second", "Z.cs(2,1)"),
            new("PX001", "error", "first", "A.cs(1,1)")
        ]);
        return 0;
}

static Task Write(
    string schemaVersion,
    string packId,
    string packVersion,
    IReadOnlyList<FixtureDiagnostic> diagnostics) =>
    Console.Out.WriteAsync(JsonSerializer.Serialize(new
    {
        schemaVersion,
        packId,
        packVersion,
        diagnostics
    }));

internal sealed record FixtureDiagnostic(string Code, string Severity, string Message, string Location);
