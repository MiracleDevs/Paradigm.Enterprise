using System.Text;
using System.Text.RegularExpressions;

namespace Paradigm.Enterprise.Cli;

internal sealed class SolutionScaffolder
{
    #region Nested Types

    private sealed record ScaffoldFile(string SourcePath, string RelativeTarget);

    #endregion

    #region Constants

    private const string TemplateName = "Paradigm.Web.ApiTemplate";
    private const string StarterScript = """
        #!/usr/bin/env bash
        set -euo pipefail

        ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)"
        PARADIGM_CLI_VERSION="__PARADIGM_VERSION__"
        APPHOST_PROJECT="${ROOT}/src/__SOLUTION_NAME__.AppHost/__SOLUTION_NAME__.AppHost.csproj"

        usage() {
            echo "Usage: ./start.sh [start|stop|doctor|help]"
            echo "  start   Start the Aspire development environment (default)"
            echo "  stop    Stop Aspire-managed resources"
            echo "  doctor  Check Paradigm, Aspire, and Docker prerequisites"
        }

        fail() {
            echo "error: $*" >&2
            exit 1
        }

        ensure_dotnet() {
            command -v dotnet >/dev/null 2>&1 || fail ".NET SDK is required. Install .NET 10 and retry."
            dotnet --list-sdks | awk '{print $1}' | grep -Eq '^10\.' || fail ".NET 10 SDK is required. Install it and retry."
        }

        require_apphost() {
            [[ -f "${APPHOST_PROJECT}" ]] || fail "Aspire AppHost was not found at ${APPHOST_PROJECT}. Complete the Paradigm Aspire setup first."
        }

        ensure_manifest() {
            if [[ ! -f "${ROOT}/.config/dotnet-tools.json" ]]; then
                echo "Creating repository-local .NET tool manifest..."
                dotnet new tool-manifest --output "${ROOT}" >/dev/null
            fi
        }

        ensure_local_tool() {
            local package="$1"
            local command="$2"
            shift 2
            if ! dotnet tool list --local | awk '{print tolower($1)}' | grep -qx "$(printf '%s' "${package}" | tr '[:upper:]' '[:lower:]')"; then
                echo "Installing ${package} in the repository-local tool manifest..."
                dotnet tool install "${package}" "$@"
            fi
            dotnet tool restore >/dev/null
            dotnet tool run "${command}" --version >/dev/null
        }

        ensure_tools() {
            ensure_dotnet
            ensure_manifest
            ensure_local_tool "Paradigm.Enterprise.Cli" "paradigm" --version "${PARADIGM_CLI_VERSION}"
            ensure_local_tool "Aspire.Cli" "aspire"
        }

        wait_for_docker() {
            while ! command -v docker >/dev/null 2>&1 || ! docker info >/dev/null 2>&1; do
                echo "Docker is missing or its daemon is not running."
                if [[ ! -t 0 ]]; then
                    fail "Start/install Docker and rerun this command from an interactive terminal."
                fi
                read -r -p "Install or start Docker, then press Enter to retry (q to quit): " answer
                [[ "${answer}" != "q" && "${answer}" != "Q" ]] || exit 1
            done
        }

        cd "${ROOT}"
        action="${1:-start}"
        case "${action}" in
            -h|--help|help)
                usage
                ;;
            start)
                ensure_tools
                wait_for_docker
                require_apphost
                dotnet tool run aspire run --apphost "${APPHOST_PROJECT}" --non-interactive --nologo
                ;;
            stop)
                ensure_tools
                require_apphost
                dotnet tool run aspire stop --apphost "${APPHOST_PROJECT}" --non-interactive --nologo
                ;;
            doctor)
                ensure_tools
                wait_for_docker
                dotnet tool run paradigm doctor --project src
                dotnet tool run aspire doctor
                ;;
            *)
                usage
                fail "Unknown action '${action}'."
                ;;
        esac
        """;

    #endregion

    #region Fields

    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "",
        ".config",
        ".cs",
        ".csproj",
        ".css",
        ".editorconfig",
        ".gitignore",
        ".html",
        ".json",
        ".md",
        ".props",
        ".ps1",
        ".resx",
        ".sh",
        ".sln",
        ".slnx",
        ".t4",
        ".targets",
        ".txt",
        ".vstemplate",
        ".xml"
    };
    private static readonly HashSet<string> ExcludedParts = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        ".vs",
        "bin",
        "obj"
    };
    private static readonly Regex NamePattern = new("^[A-Za-z_][A-Za-z0-9_]*(?:\\.[A-Za-z_][A-Za-z0-9_]*)*$", RegexOptions.CultureInvariant);
    private static readonly Regex VersionPattern = new("^[0-9A-Za-z][0-9A-Za-z.+-]*$", RegexOptions.CultureInvariant);
    private static readonly Regex GuidPattern = new("\\{[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\\}", RegexOptions.CultureInvariant);
    private static readonly Regex ParadigmPackagePattern = new("(<PackageReference\\b(?=[^>]*\\bInclude=\"Paradigm\\.Enterprise\\.[^\"]+\")[^>]*\\bVersion=\")[^\"]+(\")", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    #endregion

    #region Public Methods

    public ScaffoldResult Scaffold(ScaffoldSolutionOptions options, CancellationToken cancellationToken)
    {
        if (!NamePattern.IsMatch(options.Name))
            throw new ArgumentException("Name must be a valid dot-separated C# namespace.");
        if (!VersionPattern.IsMatch(options.ParadigmVersion))
            throw new ArgumentException("Paradigm version contains unsupported characters.");

        var source = ResolveSource(options.TemplateRoot);
        var destination = ValidateDestination(source, options.OutputPath);
        var planned = CollectFiles(source, options.Name);
        if (planned.Count == 0)
            throw new InvalidOperationException("Template source contains no files.");

        var guidMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var targets = new List<string>(planned.Count + 1);
        foreach (var file in planned)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var target = Path.Combine(destination, "src", file.RelativeTarget);
            targets.Add(target);
            if (TextExtensions.Contains(Path.GetExtension(file.SourcePath)))
            {
                var transformed = TransformText(File.ReadAllText(file.SourcePath, StrictUtf8), options.Name, options.ParadigmVersion, guidMap);
                if (!options.DryRun)
                    WriteText(target, transformed);
            }
            else if (!options.DryRun)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file.SourcePath, target);
                File.SetLastWriteTimeUtc(target, File.GetLastWriteTimeUtc(file.SourcePath));
            }
        }

        var starter = Path.Combine(destination, "start.sh");
        targets.Add(starter);
        if (!options.DryRun)
        {
            var starterText = StarterScript
                .Replace("__PARADIGM_VERSION__", options.ParadigmVersion, StringComparison.Ordinal)
                .Replace("__SOLUTION_NAME__", options.Name, StringComparison.Ordinal);
            WriteText(starter, starterText + Environment.NewLine);
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(starter, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }

        return new(options.DryRun, destination, targets);
    }

    #endregion

    #region Private Methods

    private static string ResolveSource(string templateRoot)
    {
        var root = Path.GetFullPath(templateRoot);
        var source = Directory.Exists(Path.Combine(root, "src")) ? Path.Combine(root, "src") : root;
        if (!Directory.Exists(source) || !Directory.EnumerateFiles(source).Any(path => Path.GetExtension(path) is ".sln" or ".slnx"))
            throw new DirectoryNotFoundException($"Template source does not contain a solution: {source}");
        return PathContainment.Canonicalize(source);
    }

    private static string ValidateDestination(string source, string output)
    {
        var destination = Path.GetFullPath(output);
        var relative = Path.GetRelativePath(source, destination);
        if (relative == "." || (!Path.IsPathRooted(relative) && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) && relative != ".."))
            throw new ArgumentException("Output must not be the template source or one of its descendants.");
        if (File.Exists(destination))
            throw new ArgumentException($"Output is not a directory: {destination}");
        if (Directory.Exists(destination) && Directory.EnumerateFileSystemEntries(destination).Any())
            throw new ArgumentException($"Output directory must be empty: {destination}");
        return destination;
    }

    private static IReadOnlyList<ScaffoldFile> CollectFiles(string source, string name)
    {
        return Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(source, path).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(ExcludedParts.Contains))
            .Select(path => new ScaffoldFile(path, ReplaceName(Path.GetRelativePath(source, path), name)))
            .OrderBy(file => file.RelativeTarget, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string TransformText(string text, string name, string paradigmVersion, IDictionary<string, string> guidMap)
    {
        var transformed = ReplaceName(text, name);
        transformed = ParadigmPackagePattern.Replace(transformed, match => match.Groups[1].Value + paradigmVersion + match.Groups[2].Value);
        return GuidPattern.Replace(transformed, match =>
        {
            if (!guidMap.TryGetValue(match.Value, out var replacement))
            {
                replacement = "{" + Guid.NewGuid().ToString().ToUpperInvariant() + "}";
                guidMap[match.Value] = replacement;
            }
            return replacement;
        });
    }

    private static string ReplaceName(string value, string name) => value
        .Replace(TemplateName, name, StringComparison.Ordinal)
        .Replace("$ext_safeprojectname$", name, StringComparison.Ordinal)
        .Replace("$safeprojectname$", name, StringComparison.Ordinal);

    private static void WriteText(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text, new UTF8Encoding(false));
    }

    #endregion
}