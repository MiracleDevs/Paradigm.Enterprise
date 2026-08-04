using Paradigm.Enterprise.Checks.CSharp;

namespace Paradigm.Enterprise.Cli;

internal sealed class ChecksRunCommandHandler(IProjectResolutionService projects, IAssetService assets, IConfigurationService configuration) : CliCommandHandler<ChecksRunOptions>
{
    #region Properties

    public override string Route => "checks run";

    #endregion

    #region Overrides

    public override Task<CommandResponse> ExecuteAsync(ChecksRunOptions options, CancellationToken cancellationToken)
    {
        var selection = projects.Resolve(options.Project);
        var configured = configuration.Load(selection, options.Config);
        var diagnostics = configured.Diagnostics.ToList();
        diagnostics.AddRange(SuppressionPolicy.Expired(configured.Configuration.Suppressions, configured.Configuration.Path));
        var packages = new List<PackageInfo>();
        var executed = false;
        if (!diagnostics.Any(diagnostic => diagnostic.Severity == "error" && diagnostic.Code != "PE7004"))
            foreach (var project in selection.Projects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var asset = assets.Read(project, options.Framework, true);
                    packages.AddRange(asset.Packages);
                    var results = CSharpCheckService.Analyze(new(project, asset.Framework));
                    diagnostics.AddRange(results.Select(result => new Diagnostic(result.Code, result.Severity, result.Message, result.Location)).Where(diagnostic => !SuppressionPolicy.IsSuppressed(diagnostic, configured.Configuration.Suppressions)));
                    executed = true;
                }
                catch (AssetsException exception)
                {
                    diagnostics.Add(new("PE1002", "error", exception.Message, project));
                }
            }

        BuiltInCheckInfo[] checks = [new("csharp", "PE31")];
        return Task.FromResult(ResponseFactory.Create(Route, packages, [], diagnostics, checks: new(checks, executed ? ["csharp"] : [])));
    }

    #endregion
}
