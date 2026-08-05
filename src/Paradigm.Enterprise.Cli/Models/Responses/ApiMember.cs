using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;

internal sealed record ApiMember([property: JsonPropertyName("kind")] string Kind, [property: JsonPropertyName("name")] string Name, [property: JsonPropertyName("signature")] string Signature, [property: JsonPropertyName("type")] string? Type, [property: JsonPropertyName("accessors")] IReadOnlyList<ApiAccessor> Accessors, [property: JsonPropertyName("attributes")] IReadOnlyList<string> Attributes, [property: JsonPropertyName("genericConstraints")] IReadOnlyList<ApiGenericConstraint> GenericConstraints);