namespace Paradigm.Enterprise.Cli;
internal interface IProjectResolutionService
{
    ProjectSelection Resolve(string? path);
}
