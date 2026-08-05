namespace Paradigm.Enterprise.Cli;

internal sealed class InspectCommandHandler(MetadataCommandLoader loader, IValidationService validation) : CliCommandHandler<InspectOptions>
{
    #region Properties

    public override string Route => "inspect";

    #endregion

    #region Overrides

    public override Task<CommandResponse> ExecuteAsync(InspectOptions options, CancellationToken cancellationToken)
    {
        var loaded = loader.Load(options.Project, options.Framework, true, false);
        var results = validation.Inspect(loaded.Types).Where(IsApplicationResult).ToArray();
        return Task.FromResult(ResponseFactory.Create(Route, loaded.Packages, results, loaded.Diagnostics));
    }

    #endregion

    #region Private Methods

    private static bool IsApplicationResult(ResultItem result) => result.Project is not null && !result.Project.StartsWith("Paradigm.Enterprise", StringComparison.Ordinal);

    #endregion
}