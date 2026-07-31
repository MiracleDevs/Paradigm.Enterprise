namespace Paradigm.Enterprise.Cli;

internal abstract class ApiCommandHandler<TOptions>(MetadataCommandLoader loader, IApiQueryService api) : CliCommandHandler<TOptions> where TOptions : ICliCommandOptions
{
    #region Properties

    protected MetadataCommandLoader Loader { get; } = loader;
    protected IApiQueryService Api { get; } = api;

    #endregion
}