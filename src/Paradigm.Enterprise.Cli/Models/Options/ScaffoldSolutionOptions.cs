namespace Paradigm.Enterprise.Cli;

internal sealed record ScaffoldSolutionOptions(string TemplateRoot, string Name, string OutputPath, string ParadigmVersion, bool DryRun, OutputFormat Format) : ICliCommandOptions
{
    #region Properties

    public string? Project => null;
    public string? Framework => null;

    #endregion
}