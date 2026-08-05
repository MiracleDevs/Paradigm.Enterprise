namespace Paradigm.Enterprise.Cli;

internal sealed record DatabaseValidateOptions(string ProjectPath, string? SolutionPath, bool Strict, OutputFormat Format) : ICliCommandOptions
{
    #region Properties

    public string? Project => ProjectPath;
    public string? Framework => null;

    #endregion
}