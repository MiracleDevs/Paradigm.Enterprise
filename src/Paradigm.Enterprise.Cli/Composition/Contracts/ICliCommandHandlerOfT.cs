namespace Paradigm.Enterprise.Cli;
internal interface ICliCommandHandler<in TOptions>
    where TOptions : ICliCommandOptions
{
    Task<CommandResponse> ExecuteAsync(TOptions options, CancellationToken cancellationToken);
}
