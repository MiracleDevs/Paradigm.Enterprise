using System.Text.Json;
using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;

internal interface IConfigurationService
{
    ConfigurationResult Load(ProjectSelection selection, string? requestedPath);
}