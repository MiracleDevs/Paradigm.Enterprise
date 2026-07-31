using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;
internal sealed record ApiAccessor([property: JsonPropertyName("kind")] string Kind, [property: JsonPropertyName("visibility")] string Visibility);
