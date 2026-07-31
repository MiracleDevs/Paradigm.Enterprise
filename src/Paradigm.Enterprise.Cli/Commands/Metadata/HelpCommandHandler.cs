namespace Paradigm.Enterprise.Cli;
internal sealed class HelpCommandHandler : CliCommandHandler<HelpOptions>
{
#region Properties
    public override string Route => "help";

#endregion
#region Overrides
    public override Task<CommandResponse> ExecuteAsync(HelpOptions options, CancellationToken cancellationToken) => Task.FromResult(ResponseFactory.Create(Route, [], [new("usage", "Paradigm.Enterprise CLI", CommandLine.Usage)], []));
#endregion
}
