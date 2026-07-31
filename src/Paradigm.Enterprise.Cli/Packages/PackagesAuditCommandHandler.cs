using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Paradigm.Enterprise.Cli;

internal sealed class PackagesAuditCommandHandler(IProjectResolutionService projects, IAssetService assets, IConfigurationService configuration, PackagePolicyService policy, DotNetPackageAuditor auditor) : CliCommandHandler<PackagesAuditOptions>
{
    #region Properties

    public override string Route => "packages audit";

    #endregion

    #region Overrides

    public override async Task<CommandResponse> ExecuteAsync(PackagesAuditOptions options, CancellationToken cancellationToken)
    {
        var selection = projects.Resolve(options.Project);
        var configured = configuration.Load(selection, options.Config);
        var packages = new List<PackageInfo>();
        var restored = new List<RestoredPackage>();
        var diagnostics = configured.Diagnostics.ToList();
        foreach (var project in selection.Projects)
        {
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
        if (!diagnostics.Any(x => x.Code == "PE1002"))
            diagnostics.AddRange(await auditor.AuditAsync(selection, configured.Configuration, cancellationToken));
        if (options.WarningsAsErrors)
            diagnostics = diagnostics.Select(x => x.Severity == "warning" ? x with { Severity = "error" } : x).ToList();
        return ResponseFactory.Create(Route, packages, [], diagnostics);
    }

    #endregion
}