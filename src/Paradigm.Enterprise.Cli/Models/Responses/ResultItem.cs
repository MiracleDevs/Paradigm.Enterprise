using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;
internal sealed record ResultItem([property: JsonPropertyName("kind")] string Kind, [property: JsonPropertyName("name")] string Name, [property: JsonPropertyName("detail")] string Detail, [property: JsonPropertyName("package")] string? Package = null, [property: JsonPropertyName("version")] string? Version = null, [property: JsonPropertyName("project")] string? Project = null);
