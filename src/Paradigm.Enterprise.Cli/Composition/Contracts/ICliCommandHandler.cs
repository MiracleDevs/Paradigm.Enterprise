namespace Paradigm.Enterprise.Cli;

internal interface ICliCommandHandler
{
    string Route { get; }

    Task<CommandResponse> ExecuteAsync(ICliCommandOptions options, CancellationToken cancellationToken);
}