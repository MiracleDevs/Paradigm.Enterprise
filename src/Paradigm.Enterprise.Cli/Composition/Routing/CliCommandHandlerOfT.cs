namespace Paradigm.Enterprise.Cli;
internal abstract class CliCommandHandler<TOptions> : ICliCommandHandler, ICliCommandHandler<TOptions> where TOptions : ICliCommandOptions
{
#region Properties
    public abstract string Route { get; }

#endregion
#region Public Methods
    public abstract Task<CommandResponse> ExecuteAsync(TOptions options, CancellationToken cancellationToken);
#endregion
#region Private Methods
    Task<CommandResponse> ICliCommandHandler.ExecuteAsync(ICliCommandOptions options, CancellationToken cancellationToken) => options is TOptions typed ? ExecuteAsync(typed, cancellationToken) : throw new ArgumentException($"Options for '{Route}' have the wrong type.");
#endregion
}
