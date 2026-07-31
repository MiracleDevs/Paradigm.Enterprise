namespace Paradigm.Enterprise.Cli;
internal sealed class DoctorCommandHandler(IProjectResolutionService projects, IAssetService assets) : CliCommandHandler<DoctorOptions>
{
#region Properties
    public override string Route => "doctor";

#endregion
#region Overrides
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
                results.Add(new("framework", Path.GetFileNameWithoutExtension(project), selectionAssets.Framework, Project: project));
                diagnostics.AddRange(selectionAssets.Diagnostics);
                if (selectionAssets.ApplicationAssembly is null)
                    diagnostics.Add(new("PE1002", "error", $"Build output is missing at '{selectionAssets.ExpectedApplicationAssembly ?? "the evaluated TargetPath"}'.", project));
                else if (selectionAssets.ApplicationAssemblyIsStale)
                    diagnostics.Add(new("PE1002", "error", $"Build output '{selectionAssets.ApplicationAssembly}' is older than project source or configuration.", project));
                else
                    results.Add(new("assembly", Path.GetFileName(selectionAssets.ApplicationAssembly), selectionAssets.ApplicationAssembly, Project: project));
            }
            catch (AssetsException exception)
            {
                diagnostics.Add(new("PE1002", "error", exception.Message, project));
            }
        }

        diagnostics.AddRange(ResponseFactory.VersionDiagnostics(packages));
        return Task.FromResult(ResponseFactory.Create(Route, packages, results, diagnostics));
    }
#endregion
}
