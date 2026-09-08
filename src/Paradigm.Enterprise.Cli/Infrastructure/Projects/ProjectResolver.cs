using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Paradigm.Enterprise.Cli;

internal static partial class ProjectResolver
{
    #region Public Methods

    public static ProjectSelection Resolve(string? input)
    {
        var path = Path.GetFullPath(input ?? Environment.CurrentDirectory);
        if (File.Exists(path))
        {
            return Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".csproj" => new([path], path),
                ".slnx" => new(ReadSlnx(path), path),
                ".sln" => new(ReadSln(path), path),
                _ => throw new ArgumentException($"Unsupported project path '{path}'.")
            };
        }

        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Project path '{path}' does not exist.");
        var solutions = Directory.EnumerateFiles(path, "*.sln", SearchOption.TopDirectoryOnly).Concat(Directory.EnumerateFiles(path, "*.slnx", SearchOption.TopDirectoryOnly)).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        if (solutions.Length > 1)
            throw new ArgumentException($"Multiple solutions found in '{path}'. Use --project with one solution: {string.Join(", ", solutions.Select(Path.GetFileName))}.");
        if (solutions.Length == 1)
            return Resolve(solutions[0]);
        var projects = Directory.EnumerateFiles(path, "*.csproj", SearchOption.AllDirectories).Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        if (projects.Length == 0)
            throw new ArgumentException($"No .NET project or solution found in '{path}'.");
        return new(projects, path);
    }

    #endregion

    #region Private Methods

    private static IReadOnlyList<string> ReadSlnx(string path)
    {
        var root = Path.GetDirectoryName(path)!;
        return XDocument.Load(path)
            .Descendants("Project")
            .Select(x => x.Attribute("Path")?.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x) && Path.GetExtension(x).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
            .Select(x => Path.GetFullPath(Path.Combine(root, x!)))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<string> ReadSln(string path)
    {
        var root = Path.GetDirectoryName(path)!;
        return File.ReadLines(path).Select(line => SlnProjectRegex().Match(line)).Where(match => match.Success).Select(match => Path.GetFullPath(Path.Combine(root, match.Groups[1].Value))).Where(File.Exists).Order(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    [GeneratedRegex("Project\\(\"[^\"]+\"\\)\\s*=\\s*\"[^\"]+\",\\s*\"([^\"]+\\.csproj)\"", RegexOptions.IgnoreCase)]
    private static partial Regex SlnProjectRegex();

    #endregion
}
