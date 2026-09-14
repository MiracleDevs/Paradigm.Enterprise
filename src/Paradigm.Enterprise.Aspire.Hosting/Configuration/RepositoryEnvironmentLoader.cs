using System.Text.Json;
using System.Text.RegularExpressions;

namespace Paradigm.Enterprise.Aspire.Hosting;

/// <summary>
/// Loads a repository-root environment file before the Aspire builder is created.
/// </summary>
public static partial class RepositoryEnvironmentLoader
{
    #region Public Methods

    /// <summary>
    /// Loads the repository-root <c>.env</c> file into currently missing process environment variables.
    /// </summary>
    /// <returns>The resolved repository root.</returns>
    public static string Load()
    {
        var repositoryRoot = ResolveRepositoryRoot();
        var environmentPath = Path.Combine(repositoryRoot, ".env");
        if (!File.Exists(environmentPath))
            return repositoryRoot;

        foreach (var (key, value) in Parse(environmentPath))
        {
            if (Environment.GetEnvironmentVariable(key) is null)
                Environment.SetEnvironmentVariable(key, value);
        }

        return repositoryRoot;
    }

    #endregion

    #region Private Methods

    private static Dictionary<string, string> Parse(string environmentPath)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lineNumber = 0;
        foreach (var rawLine in File.ReadLines(environmentPath))
        {
            lineNumber++;
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            if (line.StartsWith("export ", StringComparison.Ordinal))
                line = line[7..].TrimStart();

            var separator = line.IndexOf('=');
            if (separator <= 0)
                throw new InvalidOperationException($"Malformed .env entry at line {lineNumber}.");

            var key = line[..separator].Trim();
            if (!EnvironmentKeyExpression().IsMatch(key))
                throw new InvalidOperationException($"Invalid .env key at line {lineNumber}.");
            if (!values.TryAdd(key, ParseValue(line[(separator + 1)..].Trim(), lineNumber)))
                throw new InvalidOperationException($"Duplicate .env key '{key}' at line {lineNumber}.");
        }

        return values;
    }

    private static string ParseValue(string value, int lineNumber)
    {
        if (value.Length == 0 || value[0] is not ('\'' or '"'))
            return value;

        var quote = value[0];
        var closeIndex = value.IndexOf(quote, 1);
        if (closeIndex < 0)
            throw new InvalidOperationException($"Unmatched quote in .env entry at line {lineNumber}.");

        var remainder = value[(closeIndex + 1)..].TrimStart();
        if (remainder.StartsWith(';'))
            remainder = remainder[1..].TrimStart();
        if (remainder.Length > 0 && !remainder.StartsWith('#'))
            throw new InvalidOperationException($"Unexpected content after quoted .env value at line {lineNumber}.");

        return value[1..closeIndex];
    }

    private static string ResolveRepositoryRoot()
    {
        var configurationPath = FindAspireConfiguration();
        using var configuration = JsonDocument.Parse(File.ReadAllText(configurationPath));
        if (!configuration.RootElement.TryGetProperty("appHost", out var appHost) ||
            !appHost.TryGetProperty("path", out var path) ||
            path.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(path.GetString()))
            throw new InvalidOperationException("aspire.config.json must define appHost.path.");

        var repositoryRoot = Path.GetDirectoryName(configurationPath)
            ?? throw new InvalidOperationException("Unable to resolve the repository root.");
        var appHostPath = Path.GetFullPath(Path.Combine(repositoryRoot, path.GetString()!));
        if (!File.Exists(appHostPath) || !appHostPath.EndsWith(".AppHost.csproj", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("aspire.config.json does not resolve to an AppHost project.");

        return repositoryRoot;
    }

    private static string FindAspireConfiguration()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var configurationPath = Path.Combine(directory.FullName, "aspire.config.json");
            if (File.Exists(configurationPath))
                return configurationPath;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Unable to locate a repository-root aspire.config.json file.");
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex EnvironmentKeyExpression();

    #endregion
}
