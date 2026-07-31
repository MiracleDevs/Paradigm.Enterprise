using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;

internal sealed record CheckData([property: JsonPropertyName("builtIn")] IReadOnlyList<BuiltInCheckInfo> BuiltIn, [property: JsonPropertyName("executed")] IReadOnlyList<string> Executed);