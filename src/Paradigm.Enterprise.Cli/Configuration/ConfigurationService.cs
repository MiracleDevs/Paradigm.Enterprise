using System.Text.Json;
using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;
internal sealed class ConfigurationService : IConfigurationService
{
#region Fields
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
#endregion
#region Public Methods
    public ConfigurationResult Load(ProjectSelection selection, string? requestedPath)
    {
        var path = requestedPath is null ? FindConfiguration(selection) : Path.GetFullPath(requestedPath);
        var workspace = WorkspaceRoot(selection, path);
        if (path is null)
            return new(new("", workspace, new(), []), []);
        if (!File.Exists(path))
            return new(new(path, workspace, new(), []), [new("PE5001", "error", $"Check configuration '{path}' does not exist.", path)]);
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            if (!root.TryGetProperty("schemaVersion", out var schema) || schema.GetString() != "1.0")
                return new(new(path, workspace, new(), []), [new("PE5001", "error", "Configuration schemaVersion must be '1.0'.", path)]);
            var packagePolicy = root.TryGetProperty("packagePolicy", out var policy) ? JsonSerializer.Deserialize<PackagePolicyConfiguration>(policy, JsonOptions) ?? new() : new();
            var suppressions = root.TryGetProperty("suppressions", out var suppressionElement) ? JsonSerializer.Deserialize<ConfiguredSuppression[]>(suppressionElement, JsonOptions) ?? [] : [];
            var diagnostics = Validate(path, suppressions);
            return new(new(path, workspace, packagePolicy, suppressions), diagnostics);
        }
        catch (Exception exception)when (exception is JsonException or IOException)
        {
            return new(new(path, workspace, new(), []), [new("PE5001", "error", $"Configuration '{path}' is invalid: {exception.Message}", path)]);
        }
    }

#endregion
#region Private Methods
    private static IReadOnlyList<Diagnostic> Validate(string path, IReadOnlyList<ConfiguredSuppression> suppressions)
    {
        var diagnostics = new List<Diagnostic>();
        foreach (var suppression in suppressions)
            if (string.IsNullOrWhiteSpace(suppression.Code) || string.IsNullOrWhiteSpace(suppression.Reason) || (string.IsNullOrWhiteSpace(suppression.Symbol) && string.IsNullOrWhiteSpace(suppression.Location)))
                diagnostics.Add(new("PE5001", "error", "Every suppression requires code, symbol or location, reason, and expiry.", path));
        return diagnostics;
    }

    private static string? FindConfiguration(ProjectSelection selection)
    {
        var start = File.Exists(selection.DisplayPath) ? Path.GetDirectoryName(selection.DisplayPath)! : selection.DisplayPath;
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
            return Path.GetFileName(directory).Equals(".paradigm", StringComparison.OrdinalIgnoreCase) ? Path.GetDirectoryName(directory)! : directory;
        }

        var display = File.Exists(selection.DisplayPath) ? Path.GetDirectoryName(selection.DisplayPath)! : selection.DisplayPath;
        return Path.GetFullPath(display);
    }
#endregion
}
