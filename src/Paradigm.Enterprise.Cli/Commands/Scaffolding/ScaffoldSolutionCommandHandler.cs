namespace Paradigm.Enterprise.Cli;

internal sealed class ScaffoldSolutionCommandHandler(SolutionScaffolder scaffolder) : CliCommandHandler<ScaffoldSolutionOptions>
{
    #region Properties

    public override string Route => "scaffold solution";

    #endregion

    #region Overrides

    public override Task<CommandResponse> ExecuteAsync(ScaffoldSolutionOptions options, CancellationToken cancellationToken)
    {
        try
        {
            var scaffold = scaffolder.Scaffold(options, cancellationToken);
            var summary = new ResultItem("scaffold", options.Name, $"dryRun={scaffold.DryRun}\noutput={scaffold.OutputPath}\nfiles={scaffold.Files.Count}");
            var results = new[] { summary }.Concat(scaffold.Files.Select(path => new ResultItem("scaffold-file", Path.GetRelativePath(scaffold.OutputPath, path), path)));
            return Task.FromResult(ResponseFactory.Create(Route, [], results, []));
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            return Task.FromResult(ResponseFactory.Create(Route, [], [], [new("PE8101", "error", exception.Message, options.OutputPath)]));
        }
    }

    #endregion
}