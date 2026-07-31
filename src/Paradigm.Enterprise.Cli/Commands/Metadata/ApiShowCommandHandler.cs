namespace Paradigm.Enterprise.Cli;
internal sealed class ApiShowCommandHandler(MetadataCommandLoader loader, IApiQueryService api) : ApiCommandHandler<ApiShowOptions>(loader, api)
{
#region Properties
    public override string Route => "api show";

#endregion
#region Overrides
    public override Task<CommandResponse> ExecuteAsync(ApiShowOptions options, CancellationToken cancellationToken)
    {
        var loaded = Loader.Load(options.Project, options.Framework, false, true);
        var match = Api.Show(loaded.Types, options.Symbol, options.Package);
        return Task.FromResult(ResponseFactory.Create(Route, loaded.Packages, match.Results, loaded.Diagnostics.Concat(match.Diagnostics)));
    }
#endregion
}
