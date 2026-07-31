namespace Paradigm.Enterprise.Cli;

internal sealed class ProjectResolutionService : IProjectResolutionService
{
    #region Public Methods

    public ProjectSelection Resolve(string? path) => ProjectResolver.Resolve(path);

    #endregion
}