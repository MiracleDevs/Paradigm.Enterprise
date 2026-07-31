using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Paradigm.Enterprise.Cli;

internal sealed class DotNetPackageAuditor(string dotnetExecutable = "dotnet")
{
    #region Nested Types

    private sealed record AuditKind(string Option, string Code, string Severity);

    #endregion

    #region Fields

    private static readonly AuditKind[] Kinds = [new("--vulnerable", "PE7006", "error"), new("--deprecated", "PE7007", "warning"), new("--outdated", "PE7008", "warning")];

    #endregion

    #region Public Methods

    public async Task<IReadOnlyList<Diagnostic>> AuditAsync(ProjectSelection selection, ParadigmConfiguration configuration, CancellationToken cancellationToken)
    {
        var diagnostics = new List<Diagnostic>();
        foreach (var project in selection.Projects.Order(StringComparer.OrdinalIgnoreCase))
            foreach (var kind in Kinds)
            {
                var result = await RunAsync(project, kind.Option, configuration.PackagePolicy.IncludePrerelease, cancellationToken);
                if (result.ExitCode != 0)
                {
                    diagnostics.Add(new("PE7005", "error", $"Dependency audit '{kind.Option}' was incomplete for '{project}': {Bound(result.Error, 500)}", project));
                    continue;
                }

                try
                {
                    using var document = JsonDocument.Parse(ExtractJson(result.Output));
                    diagnostics.AddRange(ReadFindings(document.RootElement, kind, project, configuration.PackagePolicy.IncludePrerelease));
                }
                catch (JsonException exception)
                {
                    diagnostics.Add(new("PE7005", "error", $"Dependency audit '{kind.Option}' returned invalid JSON: {exception.Message}", project));
                }
            }

        return diagnostics.Where(x => !SuppressionPolicy.IsSuppressed(x, configuration.Suppressions)).OrderBy(x => x.Code).ThenBy(x => x.Location, StringComparer.Ordinal).ThenBy(x => x.Message, StringComparer.Ordinal).ToArray();
    }

    #endregion

    #region Private Methods

    internal static IReadOnlyList<Diagnostic> ReadFindings(string json, string option, string project, bool includePrerelease)
    {
        var kind = Kinds.Single(x => x.Option == option);
        using var document = JsonDocument.Parse(ExtractJson(json));
        return ReadFindings(document.RootElement, kind, project, includePrerelease).ToArray();
    }

    internal async Task<(int ExitCode, string Output, string Error)> RunAsync(string project, string option, bool includePrerelease, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new()
            {
                FileName = dotnetExecutable,
                WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(project))!,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                CreateNoWindow = true,
                ArgumentList =
                {
                    "package",
                    "list",
                    "--project",
                    project,
                    "--format",
                    "json",
                    "--output-version",
                    "1",
                    "--no-restore",
                    option,
                    "--include-transitive"
                }
            }
        };
        if (includePrerelease && option == "--outdated")
            process.StartInfo.ArgumentList.Add("--include-prerelease");
        process.Start();
        using var cancellationRegistration = cancellationToken.Register(() => TryKill(process));
        try
        {
            var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            return (process.ExitCode, await output, await error);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }
    }

    private static IEnumerable<Diagnostic> ReadFindings(JsonElement root, AuditKind kind, string project, bool includePrerelease)
    {
        foreach (var (item, direct) in PackageObjects(root, false))
        {
            if (!item.TryGetProperty("id", out var idElement) && !item.TryGetProperty("name", out idElement))
                continue;
            var id = idElement.GetString();
            if (string.IsNullOrWhiteSpace(id))
                continue;
            var resolved = String(item, "resolvedVersion") ?? String(item, "version") ?? "unknown";
            if (!SemanticVersion.TryParse(resolved, out var resolvedVersion) || !includePrerelease && resolvedVersion.IsPrerelease)
                continue;
            var hasVulnerabilities = item.TryGetProperty("vulnerabilities", out var vulnerabilities) && vulnerabilities.ValueKind == JsonValueKind.Array && vulnerabilities.GetArrayLength() > 0;
            var latest = String(item, "latestVersion");
            var deprecated = item.TryGetProperty("deprecationReasons", out _) || item.TryGetProperty("deprecationAlternative", out _);
            var latestVersion = latest is not null && SemanticVersion.TryParse(latest, out var parsedLatest) ? parsedLatest : null;
            var finding = kind.Option switch
            {
                "--vulnerable" => hasVulnerabilities,
                "--deprecated" => deprecated,
                "--outdated" => direct && latestVersion is not null && (includePrerelease || !latestVersion.IsPrerelease) && latestVersion.CompareTo(resolvedVersion) > 0,
                _ => false
            };
            if (!finding)
                continue;
            var detail = kind.Option == "--outdated" ? $" (latest stable {latest})" : "";
            yield return new(kind.Code, kind.Severity, $"{id} {resolved} is {kind.Option[2..]}{detail}.", project);
        }
    }

    private static IEnumerable<(JsonElement Element, bool Direct)> PackageObjects(JsonElement element, bool direct)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("id", out _) || element.TryGetProperty("name", out _))
                yield return (element, direct);
            foreach (var property in element.EnumerateObject())
            {
                var childDirect = property.NameEquals("topLevelPackages") ? true : property.NameEquals("transitivePackages") ? false : direct;
                foreach (var child in PackageObjects(property.Value, childDirect))
                    yield return child;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray())
                foreach (var child in PackageObjects(item, direct))
                    yield return child;
    }

    private static string? String(JsonElement element, string name) => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static string ExtractJson(string value)
    {
        var start = value.IndexOf('{');
        var end = value.LastIndexOf('}');
        return start >= 0 && end >= start ? value[start..(end + 1)] : value;
    }

    private static string Bound(string value, int length) => value.Length <= length ? value : value[..length] + "...";
    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Cancellation must remain observable even if the operating system already reaped the child.
        }
    }

    #endregion
}