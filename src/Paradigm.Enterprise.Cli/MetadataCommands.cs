namespace Paradigm.Enterprise.Cli;

internal sealed record LoadedMetadata(
    ProjectSelection Selection,
    IReadOnlyList<PackageInfo> Packages,
    IReadOnlyList<InspectedType> Types,
    IReadOnlyList<Diagnostic> Diagnostics);

internal sealed class MetadataCommandLoader(
    IProjectResolutionService projects,
    IAssetService assets,
    IMetadataService metadata)
{
    public LoadedMetadata Load(
        string? project,
        string? framework,
        bool requireApplicationAssembly,
        bool packagesOnly)
    {
        var selection = projects.Resolve(project);
        var packages = new List<PackageInfo>();
        var allTypes = new List<InspectedType>();
        var diagnostics = new List<Diagnostic>();
        foreach (var projectPath in selection.Projects)
        {
            try
            {
                var asset = assets.Read(projectPath, framework, packagesOnly);
                packages.AddRange(asset.Packages);
                diagnostics.AddRange(asset.Diagnostics);
                if (requireApplicationAssembly)
                {
                    if (asset.ApplicationAssembly is null)
                    {
                        diagnostics.Add(new("PE1002", "error",
                            $"Build output is missing at '{asset.ExpectedApplicationAssembly ?? "the evaluated TargetPath"}'.",
                            projectPath));
                        continue;
                    }
                    if (asset.ApplicationAssemblyIsStale)
                    {
                        diagnostics.Add(new("PE1002", "error",
                            $"Build output '{asset.ApplicationAssembly}' is stale; rebuild before analysis.",
                            projectPath));
                        continue;
                    }
                }

                var inspected = metadata.Inspect(asset);
                allTypes.AddRange(inspected.Types);
                diagnostics.AddRange(inspected.Diagnostics);
            }
            catch (Exception exception) when (exception is AssetsException or FileNotFoundException or BadImageFormatException)
            {
                diagnostics.Add(new("PE1002", "error", exception.Message, projectPath));
            }
        }

        allTypes = allTypes.DistinctBy(x => (x.FullName, x.AssemblyName)).ToList();
        diagnostics.AddRange(ResponseFactory.VersionDiagnostics(packages));
        return new(selection, packages, allTypes, diagnostics);
    }
}

internal sealed class HelpCommandHandler : CliCommandHandler<HelpOptions>
{
    public override string Route => "help";

    public override Task<CommandResponse> ExecuteAsync(HelpOptions options, CancellationToken cancellationToken) =>
        Task.FromResult(ResponseFactory.Create(Route, [], [new("usage", "Paradigm.Enterprise CLI", CommandLine.Usage)], []));
}

internal sealed class VersionCommandHandler : CliCommandHandler<VersionOptions>
{
    public override string Route => "version";

    public override Task<CommandResponse> ExecuteAsync(VersionOptions options, CancellationToken cancellationToken) =>
        Task.FromResult(ResponseFactory.Create(Route, [],
            [new("version", "Paradigm.Enterprise.Cli",
                typeof(CliApplication).Assembly.GetName().Version?.ToString(3) ?? "unknown")], []));
}

internal sealed class DoctorCommandHandler(
    IProjectResolutionService projects,
    IAssetService assets) : CliCommandHandler<DoctorOptions>
{
    public override string Route => "doctor";

    public override Task<CommandResponse> ExecuteAsync(DoctorOptions options, CancellationToken cancellationToken)
    {
        var selection = projects.Resolve(options.Project);
        var results = new List<ResultItem>
        {
            new("environment", ".NET runtime", Environment.Version.ToString()),
            new("project", selection.DisplayPath, $"{selection.Projects.Count} project(s)")
        };
        var packages = new List<PackageInfo>();
        var diagnostics = new List<Diagnostic>();
        foreach (var project in selection.Projects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var assetsPath = Path.Combine(Path.GetDirectoryName(project)!, "obj", "project.assets.json");
            if (!File.Exists(assetsPath))
            {
                diagnostics.Add(new("PE1002", "error", "Restore output is missing.", project));
                continue;
            }
            try
            {
                var selectionAssets = assets.Read(project, null);
                packages.AddRange(selectionAssets.Packages);
                results.Add(new("framework", Path.GetFileNameWithoutExtension(project), selectionAssets.Framework,
                    Project: project));
                diagnostics.AddRange(selectionAssets.Diagnostics);
                if (selectionAssets.ApplicationAssembly is null)
                    diagnostics.Add(new("PE1002", "error",
                        $"Build output is missing at '{selectionAssets.ExpectedApplicationAssembly ?? "the evaluated TargetPath"}'.",
                        project));
                else if (selectionAssets.ApplicationAssemblyIsStale)
                    diagnostics.Add(new("PE1002", "error",
                        $"Build output '{selectionAssets.ApplicationAssembly}' is older than project source or configuration.",
                        project));
                else
                    results.Add(new("assembly", Path.GetFileName(selectionAssets.ApplicationAssembly),
                        selectionAssets.ApplicationAssembly, Project: project));
            }
            catch (AssetsException exception)
            {
                diagnostics.Add(new("PE1002", "error", exception.Message, project));
            }
        }
        diagnostics.AddRange(ResponseFactory.VersionDiagnostics(packages));
        return Task.FromResult(ResponseFactory.Create(Route, packages, results, diagnostics));
    }
}

internal abstract class ApiCommandHandler<TOptions>(
    MetadataCommandLoader loader,
    IApiQueryService api) : CliCommandHandler<TOptions>
    where TOptions : ICliCommandOptions
{
    protected MetadataCommandLoader Loader { get; } = loader;
    protected IApiQueryService Api { get; } = api;
}

internal sealed class ApiSearchCommandHandler(
    MetadataCommandLoader loader,
    IApiQueryService api) : ApiCommandHandler<ApiSearchOptions>(loader, api)
{
    public override string Route => "api search";

    public override Task<CommandResponse> ExecuteAsync(ApiSearchOptions options, CancellationToken cancellationToken)
    {
        var loaded = Loader.Load(options.Project, options.Framework, false, true);
        return Task.FromResult(ResponseFactory.Create(Route, loaded.Packages,
            Api.Search(loaded.Types, options.Query, options.Package, options.Limit), loaded.Diagnostics));
    }
}

internal sealed class ApiShowCommandHandler(
    MetadataCommandLoader loader,
    IApiQueryService api) : ApiCommandHandler<ApiShowOptions>(loader, api)
{
    public override string Route => "api show";

    public override Task<CommandResponse> ExecuteAsync(ApiShowOptions options, CancellationToken cancellationToken)
    {
        var loaded = Loader.Load(options.Project, options.Framework, false, true);
        var match = Api.Show(loaded.Types, options.Symbol, options.Package);
        return Task.FromResult(ResponseFactory.Create(Route, loaded.Packages, match.Results,
            loaded.Diagnostics.Concat(match.Diagnostics)));
    }
}

internal sealed class ApiGuideCommandHandler(
    MetadataCommandLoader loader,
    IApiQueryService api,
    ApiGuideService guides) : ApiCommandHandler<ApiGuideOptions>(loader, api)
{
    public override string Route => "api guide";

    public override Task<CommandResponse> ExecuteAsync(ApiGuideOptions options, CancellationToken cancellationToken)
    {
        var loaded = Loader.Load(options.Project, options.Framework, false, true);
        var match = Api.Show(loaded.Types, options.Symbol, options.Package);
        var diagnostics = loaded.Diagnostics.Concat(match.Diagnostics).ToList();
        GuideInfo? guide = null;
        if (match.Types.Count == 1 && match.Diagnostics.All(x => x.Code != "PE0003"))
        {
            guide = guides.Create(match.Types[0]);
            if (guide is null)
                diagnostics.Add(new("PE6001", "error",
                    $"No compatible curated guide exists for '{match.Types[0].FullName}' in the resolved package version."));
        }
        return Task.FromResult(ResponseFactory.Create(Route, loaded.Packages, match.Results, diagnostics, guide));
    }
}

internal sealed class InspectCommandHandler(
    MetadataCommandLoader loader,
    IValidationService validation) : CliCommandHandler<InspectOptions>
{
    public override string Route => "inspect";

    public override Task<CommandResponse> ExecuteAsync(InspectOptions options, CancellationToken cancellationToken)
    {
        var loaded = loader.Load(options.Project, options.Framework, true, false);
        var results = validation.Inspect(loaded.Types).Where(IsApplicationResult).ToArray();
        return Task.FromResult(ResponseFactory.Create(Route, loaded.Packages, results, loaded.Diagnostics));
    }

    private static bool IsApplicationResult(ResultItem result) =>
        result.Project is not null &&
        !result.Project.StartsWith("Paradigm.Enterprise", StringComparison.Ordinal);
}

internal sealed class ValidateCommandHandler(
    MetadataCommandLoader loader,
    IValidationService validation,
    IConfigurationService configuration) : CliCommandHandler<ValidateOptions>
{
    public override string Route => "validate";

    public override Task<CommandResponse> ExecuteAsync(ValidateOptions options, CancellationToken cancellationToken)
    {
        var loaded = loader.Load(options.Project, options.Framework, true, false);
        var application = loaded.Types
            .Where(x => !x.AssemblyName.StartsWith("Paradigm.Enterprise", StringComparison.Ordinal)).ToArray();
        var results = validation.Inspect(loaded.Types)
            .Where(x => x.Project is not null &&
                        !x.Project.StartsWith("Paradigm.Enterprise", StringComparison.Ordinal)).ToArray();
        var configured = configuration.Load(loaded.Selection, null);
        var policyDiagnostics = validation.ValidateTypes(application, loaded.Types)
            .Where(diagnostic =>
                !SuppressionPolicy.IsSuppressed(diagnostic, configured.Configuration.Suppressions));
        var diagnostics = loaded.Diagnostics
            .Concat(validation.ValidateLayers(loaded.Selection))
            .Concat(configured.Diagnostics)
            .Concat(SuppressionPolicy.Expired(
                configured.Configuration.Suppressions, configured.Configuration.Path))
            .Concat(policyDiagnostics);
        return Task.FromResult(ResponseFactory.Create(Route, loaded.Packages, results, diagnostics));
    }
}

internal sealed class ApiQueryService : IApiQueryService
{
    public IReadOnlyList<ResultItem> Search(
        IEnumerable<InspectedType> types,
        string query,
        string? package,
        int limit) =>
        PublicParadigmTypes(types, package)
            .SelectMany(type =>
            {
                var items = new List<ResultItem>();
                if (type.FullName.Contains(query, StringComparison.OrdinalIgnoreCase))
                    items.Add(TypeResult(type));
                items.AddRange((type.StructuredMembers ?? [])
                    .Where(member => member.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                     member.Signature.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .Select(member => new ResultItem("member", $"{type.FullName}.{member.Name}",
                        member.Signature, type.Package, type.Version, type.AssemblyName)));
                return items;
            })
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Detail, StringComparer.Ordinal)
            .Take(limit)
            .ToArray();

    public ApiMatch Show(IEnumerable<InspectedType> types, string symbol, string? package)
    {
        var publicTypes = PublicParadigmTypes(types, package).ToArray();
        var matches = publicTypes
            .Where(x => x.FullName.Equals(symbol, StringComparison.OrdinalIgnoreCase) ||
                        x.Name.Equals(symbol, StringComparison.OrdinalIgnoreCase) ||
                        UngenericName(x.Name).Equals(symbol, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.FullName, StringComparer.Ordinal)
            .ToArray();
        if (matches.Length > 0)
            return Match(symbol, matches.Select(TypeResult).ToArray(), matches);

        var memberMatches = publicTypes.SelectMany(type => (type.StructuredMembers ?? [])
                .Where(member => member.Name.Equals(symbol, StringComparison.OrdinalIgnoreCase))
                .Select(member => (Type: type, Result: new ResultItem("member",
                    $"{type.FullName}.{member.Name}", member.Signature, type.Package, type.Version, type.AssemblyName))))
            .OrderBy(x => x.Result.Name, StringComparer.Ordinal)
            .ToArray();
        return Match(symbol, memberMatches.Select(x => x.Result).ToArray(),
            memberMatches.Select(x => x.Type).DistinctBy(x => x.FullName).ToArray());
    }

    private static ApiMatch Match(string symbol, IReadOnlyList<ResultItem> results, IReadOnlyList<InspectedType> types)
    {
        if (results.Count == 0)
            return new(results, types,
                [new("PE0001", "error", $"Symbol '{symbol}' was not found in the resolved Paradigm packages.")]);
        if (types.Count > 1)
            return new(results, types,
                [new("PE0003", "error",
                    $"Symbol '{symbol}' is ambiguous. Use a fully qualified symbol or --package. Matches: {string.Join(", ", types.Select(x => x.FullName).Order())}.")]);
        return new(results, types, []);
    }

    internal static ResultItem TypeResult(InspectedType type)
    {
        var detail = new List<string>();
        if (type.BaseType is not null)
            detail.Add($"base: {type.BaseType}");
        if (type.Interfaces.Count > 0)
            detail.Add($"interfaces: {string.Join(", ", type.Interfaces)}");
        detail.AddRange((type.StructuredMembers ?? []).Select(x => x.Signature));
        return new("type", type.FullName, string.Join('\n', detail), type.Package, type.Version, type.AssemblyName);
    }

    private static IEnumerable<InspectedType> PublicParadigmTypes(IEnumerable<InspectedType> types, string? package) =>
        types.Where(x => x.IsPublic)
            .Where(x => x.AssemblyName.StartsWith("Paradigm.Enterprise", StringComparison.OrdinalIgnoreCase))
            .Where(x => package is null ||
                        (x.Package ?? x.AssemblyName).Contains(package, StringComparison.OrdinalIgnoreCase));

    private static string UngenericName(string value)
    {
        var index = value.IndexOf('<');
        return index < 0 ? value : value[..index];
    }
}
