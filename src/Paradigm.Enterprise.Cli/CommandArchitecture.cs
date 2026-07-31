namespace Paradigm.Enterprise.Cli;

internal interface ICliCommandHandler
{
    string Route { get; }
    Task<CommandResponse> ExecuteAsync(ICliCommandOptions options, CancellationToken cancellationToken);
}

internal interface ICliCommandHandler<in TOptions> where TOptions : ICliCommandOptions
{
    Task<CommandResponse> ExecuteAsync(TOptions options, CancellationToken cancellationToken);
}

internal abstract class CliCommandHandler<TOptions> : ICliCommandHandler, ICliCommandHandler<TOptions>
    where TOptions : ICliCommandOptions
{
    public abstract string Route { get; }
    public abstract Task<CommandResponse> ExecuteAsync(TOptions options, CancellationToken cancellationToken);

    Task<CommandResponse> ICliCommandHandler.ExecuteAsync(
        ICliCommandOptions options,
        CancellationToken cancellationToken) =>
        options is TOptions typed
            ? ExecuteAsync(typed, cancellationToken)
            : throw new ArgumentException($"Options for '{Route}' have the wrong type.");
}

internal sealed class CommandRouter
{
    private readonly IReadOnlyDictionary<string, ICliCommandHandler> handlers;

    public CommandRouter(IEnumerable<ICliCommandHandler> handlers)
    {
        this.handlers = handlers.ToDictionary(x => x.Route, StringComparer.Ordinal);
    }

    public Task<CommandResponse> RouteAsync(ParsedCommand command, CancellationToken cancellationToken) =>
        handlers.TryGetValue(command.Name, out var handler)
            ? handler.ExecuteAsync(command.Options, cancellationToken)
            : throw new ArgumentException($"Unknown command '{command.Name}'.");
}

internal interface IProjectResolutionService
{
    ProjectSelection Resolve(string? path);
}

internal sealed class ProjectResolutionService : IProjectResolutionService
{
    public ProjectSelection Resolve(string? path) => ProjectResolver.Resolve(path);
}

internal interface IAssetService
{
    AssetSelection Read(string project, string? framework, bool packagesOnly = false);
}

internal sealed class AssetService : IAssetService
{
    public AssetSelection Read(string project, string? framework, bool packagesOnly = false) =>
        AssetsReader.Read(project, framework, packagesOnly);
}

internal interface IMetadataService
{
    MetadataResult Inspect(AssetSelection selection);
}

internal sealed record MetadataResult(
    IReadOnlyList<InspectedType> Types,
    IReadOnlyList<Diagnostic> Diagnostics);

internal sealed class MetadataService : IMetadataService
{
    public MetadataResult Inspect(AssetSelection selection)
    {
        using var inspector = new MetadataInspector(selection);
        var types = inspector.GetTypes();
        return new(types, inspector.Diagnostics);
    }
}

internal interface IApiQueryService
{
    IReadOnlyList<ResultItem> Search(IEnumerable<InspectedType> types, string query, string? package, int limit);
    ApiMatch Show(IEnumerable<InspectedType> types, string symbol, string? package);
}

internal sealed record ApiMatch(
    IReadOnlyList<ResultItem> Results,
    IReadOnlyList<InspectedType> Types,
    IReadOnlyList<Diagnostic> Diagnostics);

internal interface IValidationService
{
    IReadOnlyList<ResultItem> Inspect(IEnumerable<InspectedType> types);
    IReadOnlyList<Diagnostic> ValidateTypes(IEnumerable<InspectedType> application, IEnumerable<InspectedType> all);
    IReadOnlyList<Diagnostic> ValidateLayers(ProjectSelection selection);
}

internal sealed class ValidationService : IValidationService
{
    public IReadOnlyList<ResultItem> Inspect(IEnumerable<InspectedType> types) => Analysis.Inspect(types);
    public IReadOnlyList<Diagnostic> ValidateTypes(IEnumerable<InspectedType> application, IEnumerable<InspectedType> all) =>
        Analysis.ValidateTypes(application, all);
    public IReadOnlyList<Diagnostic> ValidateLayers(ProjectSelection selection) => Analysis.ValidateLayers(selection);
}

internal interface IResponseWriter
{
    Task WriteAsync(CommandResponse response, OutputFormat format, TextWriter output, CancellationToken cancellationToken);
}

internal sealed class DefaultResponseWriter : IResponseWriter
{
    public Task WriteAsync(
        CommandResponse response,
        OutputFormat format,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ResponseWriter.WriteAsync(response, format, output);
    }
}

internal interface IExitPolicy
{
    int GetExitCode(CommandResponse response);
}

internal sealed class DefaultExitPolicy : IExitPolicy
{
    public int GetExitCode(CommandResponse response)
    {
        if (response.Diagnostics.Any(x => x.Code == "PE1002" && x.Severity == "error"))
            return 3;
        return response.Diagnostics.Any(x => x.Severity == "error") ? 1 : 0;
    }
}

internal static class ResponseFactory
{
    public const string SchemaVersion = "1.1";

    public static CommandResponse Create(
        string command,
        IEnumerable<PackageInfo> packages,
        IEnumerable<ResultItem> results,
        IEnumerable<Diagnostic> diagnostics,
        GuideInfo? guide = null,
        CheckData? checks = null)
    {
        var orderedDiagnostics = diagnostics
            .DistinctBy(x => (x.Code, x.Severity, x.Message, x.Location))
            .OrderBy(x => x.Code, StringComparer.Ordinal)
            .ThenBy(x => x.Message, StringComparer.Ordinal)
            .ThenBy(x => x.Location, StringComparer.Ordinal)
            .ToArray();
        var status = orderedDiagnostics.Any(x => x.Severity == "error")
            ? "error"
            : orderedDiagnostics.Length > 0 ? "warning" : "success";
        return new(
            SchemaVersion,
            command,
            status,
            packages
                .GroupBy(x => (x.Name, x.Version))
                .Select(group =>
                {
                    var projects = group.Select(x => x.Project).Distinct(StringComparer.OrdinalIgnoreCase)
                        .Order(StringComparer.Ordinal).ToArray();
                    return new PackageInfo(group.Key.Name, group.Key.Version,
                        projects.Length == 1 ? projects[0] : $"{projects.Length} projects");
                })
                .OrderBy(x => x.Name, StringComparer.Ordinal)
                .ThenBy(x => x.Version, StringComparer.Ordinal)
                .ToArray(),
            results.OrderBy(x => x.Kind, StringComparer.Ordinal).ThenBy(x => x.Name, StringComparer.Ordinal).ToArray(),
            orderedDiagnostics,
            guide,
            checks);
    }

    public static IReadOnlyList<Diagnostic> VersionDiagnostics(IEnumerable<PackageInfo> packages)
    {
        var versions = packages.Select(x => x.Version).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToArray();
        var precedenceVersions = versions
            .Select(version => SemanticVersion.TryParse(version, out var semantic)
                ? semantic
                : null)
            .ToArray();
        var mixed = precedenceVersions.All(x => x is not null)
            ? precedenceVersions.Distinct().Count() > 1
            : versions.Length > 1;
        return !mixed
            ? []
            : [new("PE1001", "error", $"Paradigm packages use mixed versions: {string.Join(", ", versions)}.")];
    }
}
