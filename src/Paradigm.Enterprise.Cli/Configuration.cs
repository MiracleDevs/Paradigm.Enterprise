using System.Text.Json;
using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;

internal sealed record ParadigmConfiguration(
    string Path,
    string WorkspaceRoot,
    IReadOnlyList<ConfiguredCheckPack> CheckPacks,
    PackagePolicyConfiguration PackagePolicy,
    IReadOnlyList<ConfiguredSuppression> Suppressions);

internal sealed record ConfiguredCheckPack(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("executable")] string Executable,
    [property: JsonPropertyName("diagnosticPrefix")] string DiagnosticPrefix,
    [property: JsonPropertyName("timeoutSeconds")] int TimeoutSeconds = 30,
    [property: JsonPropertyName("maxOutputBytes")] int MaxOutputBytes = 1_048_576);

internal sealed record PackagePolicyConfiguration(
    [property: JsonPropertyName("minimumVersions")] IReadOnlyDictionary<string, string>? MinimumVersions = null,
    [property: JsonPropertyName("includePrerelease")] bool IncludePrerelease = false);

internal sealed record ConfiguredSuppression(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("symbol")] string? Symbol,
    [property: JsonPropertyName("location")] string? Location,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("expires")] DateOnly Expires);

internal sealed record ConfigurationResult(
    ParadigmConfiguration Configuration,
    IReadOnlyList<Diagnostic> Diagnostics);

internal interface IConfigurationService
{
    ConfigurationResult Load(ProjectSelection selection, string? requestedPath);
}

internal sealed class ConfigurationService : IConfigurationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ConfigurationResult Load(ProjectSelection selection, string? requestedPath)
    {
        var path = requestedPath is null ? FindConfiguration(selection) : Path.GetFullPath(requestedPath);
        var workspace = WorkspaceRoot(selection, path);
        if (path is null)
            return new(new("", workspace, [], new(), []), []);
        if (!File.Exists(path))
            return new(new(path, workspace, [], new(), []),
                [new("PE5001", "error", $"Check configuration '{path}' does not exist.", path)]);
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            if (!root.TryGetProperty("schemaVersion", out var schema) || schema.GetString() != "1.0")
                return new(new(path, workspace, [], new(), []),
                    [new("PE5001", "error", "Configuration schemaVersion must be '1.0'.", path)]);
            var checkPacks = root.TryGetProperty("checkPacks", out var packs)
                ? JsonSerializer.Deserialize<ConfiguredCheckPack[]>(packs, JsonOptions) ?? []
                : [];
            var packagePolicy = root.TryGetProperty("packagePolicy", out var policy)
                ? JsonSerializer.Deserialize<PackagePolicyConfiguration>(policy, JsonOptions) ?? new()
                : new();
            var suppressions = root.TryGetProperty("suppressions", out var suppressionElement)
                ? JsonSerializer.Deserialize<ConfiguredSuppression[]>(suppressionElement, JsonOptions) ?? []
                : [];
            var diagnostics = Validate(path, checkPacks, packagePolicy, suppressions);
            return new(new(path, workspace, checkPacks.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray(),
                packagePolicy, suppressions), diagnostics);
        }
        catch (Exception exception) when (exception is JsonException or IOException)
        {
            return new(new(path, workspace, [], new(), []),
                [new("PE5001", "error", $"Configuration '{path}' is invalid: {exception.Message}", path)]);
        }
    }

    private static IReadOnlyList<Diagnostic> Validate(
        string path,
        IReadOnlyList<ConfiguredCheckPack> packs,
        PackagePolicyConfiguration packagePolicy,
        IReadOnlyList<ConfiguredSuppression> suppressions)
    {
        var diagnostics = new List<Diagnostic>();
        foreach (var duplicate in packs.GroupBy(x => x.Id, StringComparer.Ordinal)
                     .Where(x => x.Count() > 1))
            diagnostics.Add(new("PE5001", "error", $"Check pack id '{duplicate.Key}' is registered more than once.", path));
        foreach (var pack in packs)
        {
            if (string.IsNullOrWhiteSpace(pack.Id) || string.IsNullOrWhiteSpace(pack.Version) ||
                string.IsNullOrWhiteSpace(pack.Executable) || string.IsNullOrWhiteSpace(pack.DiagnosticPrefix))
                diagnostics.Add(new("PE5001", "error",
                    "Every check pack requires id, pinned version, executable, and diagnosticPrefix.", path));
            else if (!SemanticVersion.TryParse(pack.Version, out _))
                diagnostics.Add(new("PE5001", "error",
                    $"Check pack '{pack.Id}' version '{pack.Version}' is not valid Semantic Versioning.", path));
            if (pack.TimeoutSeconds is < 1 or > 600 || pack.MaxOutputBytes is < 1024 or > 16_777_216)
                diagnostics.Add(new("PE5001", "error",
                    $"Check pack '{pack.Id}' has unsafe timeoutSeconds or maxOutputBytes limits.", path));
        }
        foreach (var minimum in packagePolicy.MinimumVersions ?? new Dictionary<string, string>())
            if (!SemanticVersion.TryParse(minimum.Value, out _))
                diagnostics.Add(new("PE5001", "error",
                    $"Minimum version '{minimum.Value}' for '{minimum.Key}' is not valid Semantic Versioning.", path));
        foreach (var suppression in suppressions)
            if (string.IsNullOrWhiteSpace(suppression.Code) ||
                string.IsNullOrWhiteSpace(suppression.Reason) ||
                (string.IsNullOrWhiteSpace(suppression.Symbol) && string.IsNullOrWhiteSpace(suppression.Location)))
                diagnostics.Add(new("PE5001", "error",
                    "Every suppression requires code, symbol or location, reason, and expiry.", path));
        return diagnostics;
    }

    private static string? FindConfiguration(ProjectSelection selection)
    {
        var start = File.Exists(selection.DisplayPath)
            ? Path.GetDirectoryName(selection.DisplayPath)!
            : selection.DisplayPath;
        for (var current = new DirectoryInfo(start); current is not null; current = current.Parent)
        {
            var candidate = Path.Combine(current.FullName, ".paradigm", "config.json");
            if (File.Exists(candidate))
                return candidate;
            if (Directory.Exists(Path.Combine(current.FullName, ".git")))
                break;
        }
        return null;
    }

    private static string WorkspaceRoot(ProjectSelection selection, string? configPath)
    {
        if (configPath is not null)
        {
            var directory = Path.GetDirectoryName(configPath)!;
            return Path.GetFileName(directory).Equals(".paradigm", StringComparison.OrdinalIgnoreCase)
                ? Path.GetDirectoryName(directory)!
                : directory;
        }
        var display = File.Exists(selection.DisplayPath)
            ? Path.GetDirectoryName(selection.DisplayPath)!
            : selection.DisplayPath;
        return Path.GetFullPath(display);
    }
}

internal static class SuppressionPolicy
{
    public static IReadOnlyList<Diagnostic> Expired(
        IEnumerable<ConfiguredSuppression> suppressions,
        string? location) =>
        suppressions.Where(x => x.Expires < DateOnly.FromDateTime(DateTime.UtcNow))
            .Select(x => new Diagnostic("PE7004", "error",
                $"Suppression for {x.Code} expired on {x.Expires:yyyy-MM-dd}: {x.Reason}", location))
            .OrderBy(x => x.Message, StringComparer.Ordinal)
            .ToArray();

    public static bool IsSuppressed(Diagnostic diagnostic, IEnumerable<ConfiguredSuppression> suppressions) =>
        suppressions.Any(x =>
            x.Expires >= DateOnly.FromDateTime(DateTime.UtcNow) &&
            x.Code.Equals(diagnostic.Code, StringComparison.Ordinal) &&
            (string.IsNullOrWhiteSpace(x.Location) ||
             diagnostic.Location?.Contains(x.Location, StringComparison.OrdinalIgnoreCase) == true) &&
            (string.IsNullOrWhiteSpace(x.Symbol) ||
             diagnostic.Message.Contains(x.Symbol, StringComparison.Ordinal) ||
             diagnostic.Location?.Contains(x.Symbol, StringComparison.Ordinal) == true));
}
