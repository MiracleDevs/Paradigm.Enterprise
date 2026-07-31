using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Paradigm.Enterprise.Cli;

internal sealed record RestoredPackage(string Name, string Version, string Project, bool Direct);

internal sealed class PackagesCheckCommandHandler(
    IProjectResolutionService projects,
    IAssetService assets,
    IConfigurationService configuration,
    PackagePolicyService policy) : CliCommandHandler<PackagesCheckOptions>
{
    public override string Route => "packages check";

    public override Task<CommandResponse> ExecuteAsync(
        PackagesCheckOptions options,
        CancellationToken cancellationToken)
    {
        var selection = projects.Resolve(options.Project);
        var configured = configuration.Load(selection, options.Config);
        var packages = new List<PackageInfo>();
        var restored = new List<RestoredPackage>();
        var diagnostics = configured.Diagnostics.ToList();
        foreach (var project in selection.Projects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var asset = assets.Read(project, options.Framework, true);
                packages.AddRange(asset.Packages);
                restored.AddRange(RestoredPackageReader.Read(project, asset.Framework));
            }
            catch (AssetsException exception)
            {
                diagnostics.Add(new("PE1002", "error", exception.Message, project));
            }
        }
        diagnostics.AddRange(policy.Check(packages, restored, configured.Configuration));
        var results = restored.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Version, StringComparer.Ordinal)
            .Select(x => new ResultItem(x.Direct ? "direct-package" : "transitive-package", x.Name, x.Version,
                Project: x.Project));
        return Task.FromResult(ResponseFactory.Create(Route, packages, results, diagnostics));
    }
}

internal sealed class PackagesAuditCommandHandler(
    IProjectResolutionService projects,
    IAssetService assets,
    IConfigurationService configuration,
    PackagePolicyService policy,
    DotNetPackageAuditor auditor) : CliCommandHandler<PackagesAuditOptions>
{
    public override string Route => "packages audit";

    public override async Task<CommandResponse> ExecuteAsync(
        PackagesAuditOptions options,
        CancellationToken cancellationToken)
    {
        var selection = projects.Resolve(options.Project);
        var configured = configuration.Load(selection, options.Config);
        var packages = new List<PackageInfo>();
        var restored = new List<RestoredPackage>();
        var diagnostics = configured.Diagnostics.ToList();
        foreach (var project in selection.Projects)
        {
            try
            {
                var asset = assets.Read(project, options.Framework, true);
                packages.AddRange(asset.Packages);
                restored.AddRange(RestoredPackageReader.Read(project, asset.Framework));
            }
            catch (AssetsException exception)
            {
                diagnostics.Add(new("PE1002", "error", exception.Message, project));
            }
        }
        diagnostics.AddRange(policy.Check(packages, restored, configured.Configuration));
        if (!diagnostics.Any(x => x.Code == "PE1002"))
            diagnostics.AddRange(await auditor.AuditAsync(selection, configured.Configuration, cancellationToken));
        if (options.WarningsAsErrors)
            diagnostics = diagnostics.Select(x => x.Severity == "warning" ? x with { Severity = "error" } : x).ToList();
        return ResponseFactory.Create(Route, packages, [], diagnostics);
    }
}

internal sealed class PackagePolicyService
{
    public IReadOnlyList<Diagnostic> Check(
        IReadOnlyList<PackageInfo> paradigmPackages,
        IReadOnlyList<RestoredPackage> restored,
        ParadigmConfiguration configuration)
    {
        var diagnostics = ResponseFactory.VersionDiagnostics(paradigmPackages).ToList();
        var cliVersion = typeof(CliApplication).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var frameworkVersions = paradigmPackages
            .Where(x => !x.Name.Equals("Paradigm.Enterprise.Cli", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Version)
            .DistinctBy(x => SemanticVersion.TryParse(x, out var semantic) ? (object)semantic : x)
            .ToArray();
        if (cliVersion is not null && frameworkVersions.Length == 1 &&
            CompareVersions(frameworkVersions[0], cliVersion) != 0)
            diagnostics.Add(new("PE7002", "error",
                $"Paradigm CLI {cliVersion} does not match framework version {frameworkVersions[0]}."));

        foreach (var minimum in configuration.PackagePolicy.MinimumVersions ??
                                new Dictionary<string, string>())
        {
            foreach (var package in restored.Where(x =>
                         x.Name.Equals(minimum.Key, StringComparison.OrdinalIgnoreCase) &&
                         CompareVersions(x.Version, minimum.Value) < 0))
                diagnostics.Add(new("PE7003", "error",
                    $"{package.Name} {package.Version} is below configured minimum {minimum.Value}.",
                    package.Project));
        }
        diagnostics.AddRange(SuppressionPolicy.Expired(configuration.Suppressions, configuration.Path));
        return diagnostics.Where(x => !SuppressionPolicy.IsSuppressed(x, configuration.Suppressions))
            .OrderBy(x => x.Code).ThenBy(x => x.Location, StringComparer.Ordinal).ToArray();
    }

    internal static int CompareVersions(string left, string right) =>
        SemanticVersion.Parse(left).CompareTo(SemanticVersion.Parse(right));
}

internal static class RestoredPackageReader
{
    public static IReadOnlyList<RestoredPackage> Read(string project, string framework)
    {
        var assetsPath = Path.Combine(Path.GetDirectoryName(project)!, "obj", "project.assets.json");
        using var assets = JsonDocument.Parse(File.ReadAllText(assetsPath));
        var direct = DirectPackageNames(project);
        var target = assets.RootElement.GetProperty("targets").GetProperty(framework);
        return target.EnumerateObject()
            .Where(x => x.Value.TryGetProperty("type", out var type) && type.GetString() == "package")
            .Select(x =>
            {
                var slash = x.Name.LastIndexOf('/');
                return slash < 1
                    ? null
                    : new RestoredPackage(x.Name[..slash], x.Name[(slash + 1)..], project,
                        direct.Contains(x.Name[..slash]));
            })
            .Where(x => x is not null)
            .Cast<RestoredPackage>()
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlySet<string> DirectPackageNames(string project)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var file = new FileInfo(project); file is not null; file = file.Directory?.Parent is { } parent
                 ? new FileInfo(Path.Combine(parent.FullName, "Directory.Packages.props"))
                 : null)
        {
            if (file.Exists && file.Name.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                Add(file.FullName, "PackageReference", names);
            var props = Path.Combine(file.DirectoryName!, "Directory.Packages.props");
            if (File.Exists(props))
                Add(props, "PackageReference", names);
            if (file.Directory is null || Directory.Exists(Path.Combine(file.Directory.FullName, ".git")))
                break;
        }
        return names;
    }

    private static void Add(string path, string element, ISet<string> names)
    {
        try
        {
            foreach (var item in XDocument.Load(path).Descendants(element))
                if (item.Attribute("Include")?.Value is { } name)
                    names.Add(name);
        }
        catch
        {
            // Restore metadata remains authoritative when optional project XML cannot be inspected.
        }
    }
}

internal sealed class DotNetPackageAuditor(string dotnetExecutable = "dotnet")
{
    private sealed record AuditKind(string Option, string Code, string Severity);

    private static readonly AuditKind[] Kinds =
    [
        new("--vulnerable", "PE7006", "error"),
        new("--deprecated", "PE7007", "warning"),
        new("--outdated", "PE7008", "warning")
    ];

    public async Task<IReadOnlyList<Diagnostic>> AuditAsync(
        ProjectSelection selection,
        ParadigmConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var diagnostics = new List<Diagnostic>();
        foreach (var project in selection.Projects.Order(StringComparer.OrdinalIgnoreCase))
        foreach (var kind in Kinds)
        {
            var result = await RunAsync(project, kind.Option, configuration.PackagePolicy.IncludePrerelease,
                cancellationToken);
            if (result.ExitCode != 0)
            {
                diagnostics.Add(new("PE7005", "error",
                    $"Dependency audit '{kind.Option}' was incomplete for '{project}': {Bound(result.Error, 500)}",
                    project));
                continue;
            }
            try
            {
                using var document = JsonDocument.Parse(ExtractJson(result.Output));
                diagnostics.AddRange(ReadFindings(document.RootElement, kind, project,
                    configuration.PackagePolicy.IncludePrerelease));
            }
            catch (JsonException exception)
            {
                diagnostics.Add(new("PE7005", "error",
                    $"Dependency audit '{kind.Option}' returned invalid JSON: {exception.Message}", project));
            }
        }
        return diagnostics.Where(x => !SuppressionPolicy.IsSuppressed(x, configuration.Suppressions))
            .OrderBy(x => x.Code).ThenBy(x => x.Location, StringComparer.Ordinal)
            .ThenBy(x => x.Message, StringComparer.Ordinal).ToArray();
    }

    internal static IReadOnlyList<Diagnostic> ReadFindings(
        string json,
        string option,
        string project,
        bool includePrerelease)
    {
        var kind = Kinds.Single(x => x.Option == option);
        using var document = JsonDocument.Parse(ExtractJson(json));
        return ReadFindings(document.RootElement, kind, project, includePrerelease).ToArray();
    }

    internal async Task<(int ExitCode, string Output, string Error)> RunAsync(
        string project,
        string option,
        bool includePrerelease,
        CancellationToken cancellationToken)
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
                    "package", "list", "--project", project, "--format", "json", "--output-version", "1", "--no-restore", option,
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

    private static IEnumerable<Diagnostic> ReadFindings(
        JsonElement root,
        AuditKind kind,
        string project,
        bool includePrerelease)
    {
        foreach (var (item, direct) in PackageObjects(root, false))
        {
            if (!item.TryGetProperty("id", out var idElement) &&
                !item.TryGetProperty("name", out idElement))
                continue;
            var id = idElement.GetString();
            if (string.IsNullOrWhiteSpace(id))
                continue;
            var resolved = String(item, "resolvedVersion") ?? String(item, "version") ?? "unknown";
            if (!SemanticVersion.TryParse(resolved, out var resolvedVersion) ||
                !includePrerelease && resolvedVersion.IsPrerelease)
                continue;
            var hasVulnerabilities = item.TryGetProperty("vulnerabilities", out var vulnerabilities) &&
                                     vulnerabilities.ValueKind == JsonValueKind.Array &&
                                     vulnerabilities.GetArrayLength() > 0;
            var latest = String(item, "latestVersion");
            var deprecated = item.TryGetProperty("deprecationReasons", out _) ||
                             item.TryGetProperty("deprecationAlternative", out _);
            var latestVersion = latest is not null && SemanticVersion.TryParse(latest, out var parsedLatest)
                ? parsedLatest
                : null;
            var finding = kind.Option switch
            {
                "--vulnerable" => hasVulnerabilities,
                "--deprecated" => deprecated,
                "--outdated" => direct && latestVersion is not null &&
                                (includePrerelease || !latestVersion.IsPrerelease) &&
                                latestVersion.CompareTo(resolvedVersion) > 0,
                _ => false
            };
            if (!finding)
                continue;
            var detail = kind.Option == "--outdated" ? $" (latest stable {latest})" : "";
            yield return new(kind.Code, kind.Severity,
                $"{id} {resolved} is {kind.Option[2..]}{detail}.", project);
        }
    }

    private static IEnumerable<(JsonElement Element, bool Direct)> PackageObjects(
        JsonElement element,
        bool direct)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("id", out _) || element.TryGetProperty("name", out _))
                yield return (element, direct);
            foreach (var property in element.EnumerateObject())
            {
                var childDirect = property.NameEquals("topLevelPackages")
                    ? true
                    : property.NameEquals("transitivePackages") ? false : direct;
                foreach (var child in PackageObjects(property.Value, childDirect))
                    yield return child;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray())
            foreach (var child in PackageObjects(item, direct))
                yield return child;
    }

    private static string? String(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string ExtractJson(string value)
    {
        var start = value.IndexOf('{');
        var end = value.LastIndexOf('}');
        return start >= 0 && end >= start ? value[start..(end + 1)] : value;
    }

    private static string Bound(string value, int length) =>
        value.Length <= length ? value : value[..length] + "...";

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
}
