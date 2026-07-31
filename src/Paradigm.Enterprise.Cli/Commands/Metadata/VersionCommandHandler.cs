namespace Paradigm.Enterprise.Cli;
internal sealed class VersionCommandHandler : CliCommandHandler<VersionOptions>
{
#region Properties
    public override string Route => "version";

#endregion
#region Overrides
    public override Task<CommandResponse> ExecuteAsync(VersionOptions options, CancellationToken cancellationToken) => Task.FromResult(ResponseFactory.Create(Route, [], [new("version", "Paradigm.Enterprise.Cli", typeof(CliApplication).Assembly.GetName().Version?.ToString(3) ?? "unknown")], []));
#endregion
}
