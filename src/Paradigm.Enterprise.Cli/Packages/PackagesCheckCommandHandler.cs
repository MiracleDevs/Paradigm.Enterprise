using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Paradigm.Enterprise.Cli;

internal sealed class PackagesCheckCommandHandler(IProjectResolutionService projects, IAssetService assets, IConfigurationService configuration, PackagePolicyService policy) : CliCommandHandler<PackagesCheckOptions>
{
    #region Properties

    public override string Route => "packages check";

    #endregion

    #region Overrides

    public override Task<CommandResponse> ExecuteAsync(PackagesCheckOptions options, CancellationToken cancellationToken)
    {
        var selection = projects.Resolve(options.Project);
        var configured = configuration.Load(selection, options.Config);
        var packages = new List<PackageInfo>();
        var restored = new List<RestoredPackage>();
        var diagnostics = configured.Diagnostics.ToList();
        foreach (var project in selection.Projects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var asset = assets.Read(project, options.Framework, true);
                packages.AddRange(asset.Packages);
                restored.AddRange(RestoredPackageReader.Read(project, asset.Framework));
            }
            catch (AssetsException exception)
            {
                diagnostics.Add(new("PE1002", "error", exception.Message, project));
            }
        }

        diagnostics.AddRange(policy.Check(packages, restored, configured.Configuration));
        var results = restored.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Version, StringComparer.Ordinal).Select(x => new ResultItem(x.Direct ? "direct-package" : "transitive-package", x.Name, x.Version, Project: x.Project));
        return Task.FromResult(ResponseFactory.Create(Route, packages, results, diagnostics));
    }

    #endregion
}