namespace Paradigm.Enterprise.Cli;

internal sealed class DatabaseValidateCommandHandler(DatabaseProjectValidator validator) : CliCommandHandler<DatabaseValidateOptions>
{
    #region Properties

    public override string Route => "database validate";

    #endregion

    #region Overrides

    public override Task<CommandResponse> ExecuteAsync(DatabaseValidateOptions options, CancellationToken cancellationToken)
    {
        try
        {
            var diagnostics = validator.Validate(options, cancellationToken);
            var result = new ResultItem("database-project", Path.GetFileName(options.ProjectPath), Path.GetFullPath(options.ProjectPath));
            return Task.FromResult(ResponseFactory.Create(Route, [], [result], diagnostics));
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            return Task.FromResult(ResponseFactory.Create(Route, [], [], [new("PEDB001", "error", exception.Message, options.ProjectPath)]));
        }
    }

    #endregion
}