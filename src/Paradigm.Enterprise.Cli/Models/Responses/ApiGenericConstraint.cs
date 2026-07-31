using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;
internal sealed record ApiGenericConstraint([property: JsonPropertyName("parameter")] string Parameter, [property: JsonPropertyName("constraints")] IReadOnlyList<string> Constraints);
