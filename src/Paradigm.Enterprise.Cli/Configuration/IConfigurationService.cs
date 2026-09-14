namespace Paradigm.Enterprise.Cli;

internal interface IConfigurationService
{
    ConfigurationResult Load(ProjectSelection selection, string? requestedPath);
}