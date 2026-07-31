using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;

internal sealed record BuiltInCheckInfo([property: JsonPropertyName("id")] string Id, [property: JsonPropertyName("diagnosticPrefix")] string DiagnosticPrefix);