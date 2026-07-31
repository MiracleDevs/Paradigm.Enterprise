namespace Paradigm.Enterprise.Cli;
internal sealed class ChecksListCommandHandler : CliCommandHandler<ChecksListOptions>
{
#region Properties
    public override string Route => "checks list";

#endregion
#region Overrides
    public override Task<CommandResponse> ExecuteAsync(ChecksListOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BuiltInCheckInfo[] checks = [new("csharp", "PE31")];
        var results = checks.Select(check => new ResultItem("built-in-check", check.Id, $"diagnosticPrefix={check.DiagnosticPrefix}"));
        return Task.FromResult(ResponseFactory.Create(Route, [], results, [], checks: new(checks, [])));
    }
#endregion
}
