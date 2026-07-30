namespace Paradigm.Enterprise.Cli;

internal static class CliApplication
{
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (!CommandLine.TryParse(args, out var command, out var parseError))
        {
            await error.WriteLineAsync(parseError);
            await error.WriteLineAsync(CommandLine.Usage);
            return 2;
        }

        if (command!.Name == "help")
        {
            await output.WriteLineAsync(CommandLine.Usage);
            return 0;
        }
        if (command.Name == "version")
        {
            await output.WriteLineAsync(typeof(CliApplication).Assembly.GetName().Version?.ToString(3)
                                        ?? "unknown");
            return 0;
        }

        try
        {
            var selection = ProjectResolver.Resolve(command.Project);
            var response = command.Name == "doctor"
                ? Doctor(selection)
                : ExecuteMetadataCommand(command, selection);
            await ResponseWriter.WriteAsync(response, command.Format, output);
            if (response.Diagnostics.Any(x => x.Code == "PE1002" && x.Severity == "error"))
                return 3;
            return response.Diagnostics.Any(x => x.Severity == "error") ? 1 : 0;
        }
        catch (ArgumentException exception)
        {
            if (command.Format == OutputFormat.Json)
                await ResponseWriter.WriteAsync(
                    Response(command.Name, [], [], [new("PE0002", "error", exception.Message)]),
                    command.Format,
                    output);
            else
                await error.WriteLineAsync(exception.Message);
            return 2;
        }
        catch (Exception exception) when (exception is AssetsException or DirectoryNotFoundException or FileNotFoundException or BadImageFormatException)
        {
            var response = Response(command.Name, [], [], [new("PE1002", "error", exception.Message)]);
            await ResponseWriter.WriteAsync(response, command.Format, output);
            return 3;
        }
        catch (Exception exception)
        {
            var response = Response(command.Name, [], [], [new("PE1002", "error", $"Metadata resolution failed: {exception.Message}")]);
            await ResponseWriter.WriteAsync(response, command.Format, output);
            return 3;
        }
    }

    private static CommandResponse Doctor(ProjectSelection selection)
    {
        var results = new List<ResultItem>
        {
            new("environment", ".NET runtime", Environment.Version.ToString()),
            new("project", selection.DisplayPath, $"{selection.Projects.Count} project(s)")
        };
        var packages = new List<PackageInfo>();
        var diagnostics = new List<Diagnostic>();

        foreach (var project in selection.Projects)
        {
            var assetsPath = Path.Combine(Path.GetDirectoryName(project)!, "obj", "project.assets.json");
            if (!File.Exists(assetsPath))
            {
                diagnostics.Add(new("PE1002", "error", "Restore output is missing.", project));
                continue;
            }
            try
            {
                var assets = AssetsReader.Read(project, null);
                packages.AddRange(assets.Packages);
                results.Add(new("framework", Path.GetFileNameWithoutExtension(project), assets.Framework, Project: project));
                diagnostics.AddRange(assets.Diagnostics);
                if (assets.ApplicationAssembly is null)
                    diagnostics.Add(new("PE1002", "error",
                        $"Build output is missing at '{assets.ExpectedApplicationAssembly ?? "the evaluated TargetPath"}'.",
                        project));
                else if (assets.ApplicationAssemblyIsStale)
                    diagnostics.Add(new("PE1002", "error",
                        $"Build output '{assets.ApplicationAssembly}' is older than project source or configuration.",
                        project));
                else
                    results.Add(new("assembly", Path.GetFileName(assets.ApplicationAssembly), assets.ApplicationAssembly, Project: project));
            }
            catch (AssetsException exception)
            {
                diagnostics.Add(new("PE1002", "error", exception.Message, project));
            }
        }

        diagnostics.AddRange(VersionDiagnostics(packages));
        return Response("doctor", packages, results, diagnostics);
    }

    private static CommandResponse ExecuteMetadataCommand(ParsedCommand command, ProjectSelection selection)
    {
        var packages = new List<PackageInfo>();
        var allTypes = new List<InspectedType>();
        var diagnostics = new List<Diagnostic>();
        foreach (var project in selection.Projects)
        {
            try
            {
                var assets = AssetsReader.Read(project, command.Framework);
                packages.AddRange(assets.Packages);
                diagnostics.AddRange(assets.Diagnostics);
                if (command.Name is "inspect" or "validate")
                {
                    if (assets.ApplicationAssembly is null)
                    {
                        diagnostics.Add(new("PE1002", "error",
                            $"Build output is missing at '{assets.ExpectedApplicationAssembly ?? "the evaluated TargetPath"}'.",
                            project));
                        continue;
                    }
                    if (assets.ApplicationAssemblyIsStale)
                    {
                        diagnostics.Add(new("PE1002", "error",
                            $"Build output '{assets.ApplicationAssembly}' is stale; rebuild before {command.Name}.",
                            project));
                        continue;
                    }
                }

                using var inspector = new MetadataInspector(assets);
                allTypes.AddRange(inspector.GetTypes());
                diagnostics.AddRange(inspector.Diagnostics);
            }
            catch (Exception exception) when (exception is AssetsException or FileNotFoundException or BadImageFormatException)
            {
                diagnostics.Add(new("PE1002", "error", exception.Message, project));
            }
        }

        allTypes = allTypes.DistinctBy(x => (x.FullName, x.AssemblyName)).ToList();
        diagnostics.AddRange(VersionDiagnostics(packages));
        IReadOnlyList<ResultItem> results;
        switch (command.Name)
        {
            case "api search":
                results = Search(allTypes, command.Query!, command.Package, command.Limit);
                break;
            case "api show":
                results = Show(allTypes, command.Query!);
                if (results.Count == 0)
                    diagnostics.Add(new("PE0001", "error", $"Symbol '{command.Query}' was not found in the resolved Paradigm packages."));
                break;
            case "inspect":
                results = Analysis.Inspect(allTypes);
                results = results.Where(x => x.Project is not null &&
                    !x.Project.StartsWith("Paradigm.Enterprise", StringComparison.Ordinal)).ToArray();
                break;
            case "validate":
                var applicationTypes = allTypes.Where(x =>
                    !x.AssemblyName.StartsWith("Paradigm.Enterprise", StringComparison.Ordinal)).ToArray();
                results = Analysis.Inspect(allTypes)
                    .Where(x => x.Project is not null &&
                        !x.Project.StartsWith("Paradigm.Enterprise", StringComparison.Ordinal)).ToArray();
                diagnostics.AddRange(Analysis.ValidateLayers(selection));
                diagnostics.AddRange(Analysis.ValidateTypes(applicationTypes, allTypes));
                break;
            default:
                throw new ArgumentException($"Unknown command '{command.Name}'.");
        }

        return Response(command.Name, packages, results, diagnostics);
    }

    private static IReadOnlyList<ResultItem> Search(IEnumerable<InspectedType> types, string query, string? package, int limit)
    {
        return types
            .Where(x => x.IsPublic)
            .Where(x => x.AssemblyName.StartsWith("Paradigm.Enterprise", StringComparison.OrdinalIgnoreCase))
            .Where(x => package is null || (x.Package ?? x.AssemblyName).Contains(package, StringComparison.OrdinalIgnoreCase))
            .SelectMany(type =>
            {
                var items = new List<ResultItem>();
                if (type.FullName.Contains(query, StringComparison.OrdinalIgnoreCase))
                    items.Add(TypeResult(type));
                items.AddRange(type.Members
                    .Where(member => member.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .Select(member => new ResultItem("member", $"{type.FullName}.{MemberName(member)}", member, type.Package, type.Version, type.AssemblyName)));
                return items;
            })
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Detail, StringComparer.Ordinal)
            .Take(limit)
            .ToArray();
    }

    private static IReadOnlyList<ResultItem> Show(IEnumerable<InspectedType> types, string symbol)
    {
        var matches = types
            .Where(x => x.IsPublic)
            .Where(x => x.AssemblyName.StartsWith("Paradigm.Enterprise", StringComparison.OrdinalIgnoreCase))
            .Where(x => x.FullName.Equals(symbol, StringComparison.OrdinalIgnoreCase) ||
                        x.Name.Equals(symbol, StringComparison.OrdinalIgnoreCase) ||
                        UngenericName(x.Name).Equals(symbol, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.FullName, StringComparer.Ordinal)
            .Select(TypeResult)
            .ToArray();
        if (matches.Length > 0)
            return matches;

        return types.Where(x => x.IsPublic && x.AssemblyName.StartsWith("Paradigm.Enterprise", StringComparison.OrdinalIgnoreCase))
            .SelectMany(type => type.Members
                .Where(member => MemberName(member).Equals(symbol, StringComparison.OrdinalIgnoreCase))
                .Select(member => new ResultItem("member", $"{type.FullName}.{MemberName(member)}", member, type.Package, type.Version, type.AssemblyName)))
            .OrderBy(x => x.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static ResultItem TypeResult(InspectedType type)
    {
        var detail = new List<string>();
        if (type.BaseType is not null)
            detail.Add($"base: {type.BaseType}");
        if (type.Interfaces.Count > 0)
            detail.Add($"interfaces: {string.Join(", ", type.Interfaces)}");
        detail.AddRange(type.Members);
        return new("type", type.FullName, string.Join('\n', detail), type.Package, type.Version, type.AssemblyName);
    }

    private static string MemberName(string signature)
    {
        var beforeParameters = signature.Split('(')[0];
        var beforeProperty = beforeParameters.Split('{')[0];
        return beforeProperty.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? signature;
    }

    private static string UngenericName(string value)
    {
        var index = value.IndexOf('<');
        return index < 0 ? value : value[..index];
    }

    private static IReadOnlyList<Diagnostic> VersionDiagnostics(IEnumerable<PackageInfo> packages)
    {
        var versions = packages.Select(x => x.Version).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToArray();
        return versions.Length <= 1
            ? []
            : [new("PE1001", "error", $"Paradigm packages use mixed versions: {string.Join(", ", versions)}.")];
    }

    private static CommandResponse Response(
        string command,
        IEnumerable<PackageInfo> packages,
        IEnumerable<ResultItem> results,
        IEnumerable<Diagnostic> diagnostics)
    {
        var orderedDiagnostics = diagnostics.OrderBy(x => x.Code).ThenBy(x => x.Message, StringComparer.Ordinal).ToArray();
        var status = orderedDiagnostics.Any(x => x.Severity == "error")
            ? "error"
            : orderedDiagnostics.Length > 0 ? "warning" : "success";
        return new(
            "1.0",
            command,
            status,
            packages
                .GroupBy(x => (x.Name, x.Version))
                .Select(group =>
                {
                    var projects = group.Select(x => x.Project).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.Ordinal).ToArray();
                    return new PackageInfo(group.Key.Name, group.Key.Version, projects.Length == 1 ? projects[0] : $"{projects.Length} projects");
                })
                .OrderBy(x => x.Name, StringComparer.Ordinal)
                .ThenBy(x => x.Version, StringComparer.Ordinal)
                .ToArray(),
            results.OrderBy(x => x.Kind, StringComparer.Ordinal).ThenBy(x => x.Name, StringComparer.Ordinal).ToArray(),
            orderedDiagnostics);
    }
}
