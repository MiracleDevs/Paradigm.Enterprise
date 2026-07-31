namespace Paradigm.Enterprise.Cli;

internal sealed class MetadataCommandLoader(IProjectResolutionService projects, IAssetService assets, IMetadataService metadata)
{
    #region Public Methods

    public LoadedMetadata Load(string? project, string? framework, bool requireApplicationAssembly, bool packagesOnly)
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
                        diagnostics.Add(new("PE1002", "error", $"Build output is missing at '{asset.ExpectedApplicationAssembly ?? "the evaluated TargetPath"}'.", projectPath));
                        continue;
                    }

                    if (asset.ApplicationAssemblyIsStale)
                    {
                        diagnostics.Add(new("PE1002", "error", $"Build output '{asset.ApplicationAssembly}' is stale; rebuild before analysis.", projectPath));
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

    #endregion
}