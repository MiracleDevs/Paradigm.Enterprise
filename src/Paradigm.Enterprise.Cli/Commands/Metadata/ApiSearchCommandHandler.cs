namespace Paradigm.Enterprise.Cli;

internal sealed class ApiSearchCommandHandler(MetadataCommandLoader loader, IApiQueryService api) : ApiCommandHandler<ApiSearchOptions>(loader, api)
{
    #region Properties

    public override string Route => "api search";

    #endregion

    #region Overrides

    public override Task<CommandResponse> ExecuteAsync(ApiSearchOptions options, CancellationToken cancellationToken)
    {
        var loaded = Loader.Load(options.Project, options.Framework, false, true);
        return Task.FromResult(ResponseFactory.Create(Route, loaded.Packages, Api.Search(loaded.Types, options.Query, options.Package, options.Limit), loaded.Diagnostics));
    }

    #endregion
}