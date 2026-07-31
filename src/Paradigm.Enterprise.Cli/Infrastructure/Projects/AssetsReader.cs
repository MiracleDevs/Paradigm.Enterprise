using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Paradigm.Enterprise.Cli;
internal static partial class AssetsReader
{
#region Public Methods
    public static AssetSelection Read(string project, string? requestedFramework, bool packagesOnly = false)
    {
        var projectDirectory = Path.GetDirectoryName(project)!;
        var assetsPath = Path.Combine(projectDirectory, "obj", "project.assets.json");
        if (!File.Exists(assetsPath))
            throw new AssetsException($"Restore '{project}' before running this command.");
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(assetsPath));
        }
        catch (JsonException exception)
        {
            throw new AssetsException($"Restore metadata '{assetsPath}' is malformed: {exception.Message}");
        }

        using (document)
        {
            var root = document.RootElement;
            if (!root.TryGetProperty("targets", out var targetsElement))
                throw new AssetsException($"Restore metadata '{assetsPath}' has no targets.");
            var targets = targetsElement.EnumerateObject().Select(x => x.Name).ToArray();
            var framework = SelectFramework(targets, requestedFramework);
            var target = targetsElement.GetProperty(framework);
            var packageFolders = root.TryGetProperty("packageFolders", out var folders) ? folders.EnumerateObject().Select(x => x.Name).Order(StringComparer.OrdinalIgnoreCase).ToArray() : [];
            var packages = new List<PackageInfo>();
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var owners = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase);
            var inspectionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var libraryTypes = ReadLibraryTypes(root);
            foreach (var library in target.EnumerateObject().OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
            {
                var slash = library.Name.LastIndexOf('/');
                if (slash < 1)
                    continue;
                var name = library.Name[..slash];
                var version = library.Name[(slash + 1)..];
                var isParadigm = name.StartsWith("Paradigm.Enterprise", StringComparison.OrdinalIgnoreCase);
                if (isParadigm)
                {
                    packages.Add(new(name, version, project));
                    inspectionNames.Add(name);
                }

                if (!packagesOnly && libraryTypes.GetValueOrDefault(library.Name) == "project")
                    inspectionNames.Add(name);
                if (!library.Value.TryGetProperty("compile", out var compile))
                    continue;
                foreach (var asset in compile.EnumerateObject())
                {
                    if (!asset.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || asset.Name.EndsWith("_._", StringComparison.Ordinal))
                        continue;
                    foreach (var folder in packageFolders)
                    {
                        var candidate = Path.Combine(folder, name.ToLowerInvariant(), version, asset.Name.Replace('/', Path.DirectorySeparatorChar));
                        if (!File.Exists(candidate))
                            continue;
                        paths.Add(candidate);
                        if (isParadigm)
                            owners[Path.GetFileNameWithoutExtension(candidate)] = (name, version);
                        break;
                    }
                }
            }

            var projectName = ReadAssemblyName(project);
            if (!packagesOnly || projectName.StartsWith("Paradigm.Enterprise", StringComparison.OrdinalIgnoreCase))
                inspectionNames.Add(projectName);
            if (projectName.StartsWith("Paradigm.Enterprise", StringComparison.OrdinalIgnoreCase) && packages.All(x => !x.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase)))
            {
                var projectDocument = XDocument.Load(project);
                var projectVersion = projectDocument.Descendants("Version").Select(x => x.Value).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(projectVersion))
                    packages.Add(new(projectName, projectVersion, project));
            }

            var output = ResolveApplicationAssembly(project, framework);
            if (output.ExistingPath is not null)
            {
                paths.Add(output.ExistingPath);
                var outputDirectory = Path.GetDirectoryName(output.ExistingPath)!;
                foreach (var sibling in Directory.EnumerateFiles(outputDirectory, "*.dll").Order(StringComparer.OrdinalIgnoreCase))
                {
                    paths.Add(sibling);
                    var assemblyName = Path.GetFileNameWithoutExtension(sibling);
                    var package = packages.FirstOrDefault(x => x.Name.Equals(assemblyName, StringComparison.OrdinalIgnoreCase));
                    if (package is not null)
                        owners[assemblyName] = (package.Name, package.Version);
                }
            }

            AddTrustedPlatformAssemblies(paths);
            AddFrameworkReferenceAssemblies(paths, framework);
            var diagnostics = new List<Diagnostic>();
            var normalized = NormalizeMetadataPaths(paths, output.ExistingPath, owners, inspectionNames, diagnostics);
            return new(project, framework, packages, normalized, owners, inspectionNames, output.ExistingPath, output.ExpectedPath, output.IsStale, diagnostics);
        }
    }

#endregion
#region Private Methods
    internal static string SelectFramework(IEnumerable<string> targetValues, string? requested)
    {
        var targets = targetValues.Where(x => x.StartsWith("net", StringComparison.OrdinalIgnoreCase)).OrderByDescending(FrameworkKey, StringComparer.OrdinalIgnoreCase).ThenBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
        if (targets.Length == 0)
            throw new AssetsException("No restored net* target framework was found.");
        if (requested is null)
            return targets[0];
        var exact = targets.FirstOrDefault(x => x.Equals(requested, StringComparison.OrdinalIgnoreCase) || x.StartsWith(requested + "/", StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
            return exact;
        var requestedVersion = NetVersion(requested);
        var compatible = requestedVersion is null ? null : targets.FirstOrDefault(x => NetVersion(x)is { } candidate && candidate <= requestedVersion);
        return compatible ?? throw new AssetsException($"Framework '{requested}' has no compatible restored target. Available targets: {string.Join(", ", targets)}.");
    }

    internal static IReadOnlyList<string> NormalizeMetadataPaths(IEnumerable<string> paths, string? applicationAssembly, IReadOnlyDictionary<string, (string Name, string Version)> owners, IReadOnlySet<string> inspectionAssemblyNames, ICollection<Diagnostic> diagnostics)
    {
        var valid = new List<(string Path, string Identity, int Rank)>();
        foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var name = AssemblyName.GetAssemblyName(path);
                valid.Add((path, name.FullName ?? name.Name ?? path, PathPreference(path, applicationAssembly, owners)));
            }
            catch (Exception exception)when (exception is BadImageFormatException or FileLoadException or FileNotFoundException)
            {
                if (inspectionAssemblyNames.Contains(Path.GetFileNameWithoutExtension(path)))
                    diagnostics.Add(new("PE1002", "error", $"Metadata assembly '{path}' cannot be read: {exception.Message}", path));
            }
        }

        return valid.GroupBy(x => x.Identity, StringComparer.OrdinalIgnoreCase).Select(group => group.OrderBy(x => x.Rank).ThenBy(x => x.Path, StringComparer.OrdinalIgnoreCase).First().Path).Order(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static IReadOnlyDictionary<string, string> ReadLibraryTypes(JsonElement root)
    {
        if (!root.TryGetProperty("libraries", out var libraries))
            return new Dictionary<string, string>();
        return libraries.EnumerateObject().ToDictionary(x => x.Name, x => x.Value.TryGetProperty("type", out var type) ? type.GetString() ?? "" : "", StringComparer.OrdinalIgnoreCase);
    }

    private static int PathPreference(string path, string? applicationAssembly, IReadOnlyDictionary<string, (string Name, string Version)> owners)
    {
        if (path.Equals(applicationAssembly, StringComparison.OrdinalIgnoreCase))
            return 0;
        var name = Path.GetFileNameWithoutExtension(path);
        if (owners.ContainsKey(name))
            return 1;
        var runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location);
        if (runtimeDirectory is not null && Path.GetDirectoryName(path)?.Equals(runtimeDirectory, StringComparison.OrdinalIgnoreCase) == true)
            return 2;
        return 3;
    }

    private static string FrameworkKey(string value)
    {
        var tfm = value.Split('/')[0];
        var version = NetVersion(tfm);
        return version is null ? $"0000.0000.{tfm}" : $"{version.Major:D4}.{version.Minor:D4}.{tfm}";
    }

    private static Version? NetVersion(string value)
    {
        var match = NetFrameworkRegex().Match(value.Split('/')[0]);
        if (!match.Success)
            return null;
        return Version.TryParse(match.Groups[1].Value.Contains('.') ? match.Groups[1].Value : match.Groups[1].Value + ".0", out var version) ? version : null;
    }

    private static (string? ExistingPath, string? ExpectedPath, bool IsStale) ResolveApplicationAssembly(string project, string framework)
    {
        var tfm = framework.Split('/')[0];
        var candidates = new List<string>();
        foreach (var configuration in new[]
        {
            "Debug",
            "Release"
        }

        )
        {
            var targetPath = EvaluateTargetPath(project, tfm, configuration);
            if (!string.IsNullOrWhiteSpace(targetPath))
                candidates.Add(Path.IsPathRooted(targetPath) ? Path.GetFullPath(targetPath) : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(project)!, targetPath)));
        }

        if (candidates.Count == 0)
        {
            var fallback = Path.Combine(Path.GetDirectoryName(project)!, "bin", "Debug", tfm, ReadAssemblyName(project) + ".dll");
            candidates.Add(fallback);
        }

        var existing = candidates.Where(File.Exists).OrderByDescending(File.GetLastWriteTimeUtc).ThenBy(x => x, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        var expected = existing ?? candidates[0];
        return (existing, expected, existing is not null && IsStale(project, existing));
    }

    private static string? EvaluateTargetPath(string project, string framework, string configuration)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo { FileName = "dotnet", UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8, CreateNoWindow = true, ArgumentList = { "msbuild", project, "-nologo", "-getProperty:TargetPath", $"-property:TargetFramework={framework}", $"-property:Configuration={configuration}" } });
            if (process is null)
                return null;
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return process.ExitCode == 0 ? output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).LastOrDefault() : null;
        }
        catch (Exception exception)when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    private static bool IsStale(string project, string assembly)
    {
        var outputTime = File.GetLastWriteTimeUtc(assembly);
        if (File.GetLastWriteTimeUtc(project) > outputTime)
            return true;
        var root = Path.GetDirectoryName(project)!;
        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories).Where(path => !HasSegment(path, "bin") && !HasSegment(path, "obj")).Any(path => File.GetLastWriteTimeUtc(path) > outputTime);
    }

    private static bool HasSegment(string path, string segment) => path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(value => value.Equals(segment, StringComparison.OrdinalIgnoreCase));
    private static string ReadAssemblyName(string project)
    {
        try
        {
            return XDocument.Load(project).Descendants("AssemblyName").Select(x => x.Value).FirstOrDefault() ?? Path.GetFileNameWithoutExtension(project);
        }
        catch
        {
            return Path.GetFileNameWithoutExtension(project);
        }
    }

    private static void AddTrustedPlatformAssemblies(HashSet<string> paths)
    {
        var trusted = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (trusted is null)
            return;
        foreach (var path in trusted.Split(Path.PathSeparator))
            paths.Add(path);
    }

    private static void AddFrameworkReferenceAssemblies(HashSet<string> paths, string framework)
    {
        var runtimeDirectory = new DirectoryInfo(Path.GetDirectoryName(typeof(object).Assembly.Location)!);
        var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT") ?? runtimeDirectory.Parent?.Parent?.Parent?.FullName;
        if (dotnetRoot is null)
            return;
        var packsRoot = Path.Combine(dotnetRoot, "packs");
        if (!Directory.Exists(packsRoot))
            return;
        var tfm = framework.Split('/')[0];
        foreach (var packName in new[]
        {
            "Microsoft.NETCore.App.Ref",
            "Microsoft.AspNetCore.App.Ref",
            "NETStandard.Library.Ref"
        }

        )
        {
            var packRoot = Path.Combine(packsRoot, packName);
            if (!Directory.Exists(packRoot))
                continue;
            var referenceDirectory = Directory.EnumerateDirectories(packRoot).OrderByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase).Select(version => Path.Combine(version, "ref", tfm)).FirstOrDefault(Directory.Exists);
            if (referenceDirectory is null)
                continue;
            foreach (var assembly in Directory.EnumerateFiles(referenceDirectory, "*.dll"))
                paths.Add(assembly);
        }
    }

    [GeneratedRegex("^net(\\d+(?:\\.\\d+)?)", RegexOptions.IgnoreCase)]
    private static partial Regex NetFrameworkRegex();
#endregion
}
