using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;
internal sealed record PackageInfo([property: JsonPropertyName("name")] string Name, [property: JsonPropertyName("version")] string Version, [property: JsonPropertyName("project")] string Project);
