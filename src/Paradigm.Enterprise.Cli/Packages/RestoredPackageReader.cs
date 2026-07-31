using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Paradigm.Enterprise.Cli;

internal static class RestoredPackageReader
{
    #region Public Methods

    public static IReadOnlyList<RestoredPackage> Read(string project, string framework)
    {
        var assetsPath = Path.Combine(Path.GetDirectoryName(project)!, "obj", "project.assets.json");
        using var assets = JsonDocument.Parse(File.ReadAllText(assetsPath));
        var direct = DirectPackageNames(project);
        var target = assets.RootElement.GetProperty("targets").GetProperty(framework);
        return target.EnumerateObject().Where(x => x.Value.TryGetProperty("type", out var type) && type.GetString() == "package").Select(x =>
        {
            var slash = x.Name.LastIndexOf('/');
            return slash < 1 ? null : new RestoredPackage(x.Name[..slash], x.Name[(slash + 1)..], project, direct.Contains(x.Name[..slash]));
        }).Where(x => x is not null).Cast<RestoredPackage>().OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    #endregion

    #region Private Methods

    private static IReadOnlySet<string> DirectPackageNames(string project)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var file = new FileInfo(project); file is not null; file = file.Directory?.Parent is { } parent ? new FileInfo(Path.Combine(parent.FullName, "Directory.Packages.props")) : null)
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

    #endregion
}