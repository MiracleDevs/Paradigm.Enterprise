namespace Paradigm.Enterprise.Cli;
internal sealed class ApiGuideCommandHandler(MetadataCommandLoader loader, IApiQueryService api, ApiGuideService guides) : ApiCommandHandler<ApiGuideOptions>(loader, api)
{
#region Properties
    public override string Route => "api guide";

#endregion
#region Overrides
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
                diagnostics.Add(new("PE6001", "error", $"No compatible curated guide exists for '{match.Types[0].FullName}' in the resolved package version."));
        }

        return Task.FromResult(ResponseFactory.Create(Route, loaded.Packages, match.Results, diagnostics, guide));
    }
#endregion
}
