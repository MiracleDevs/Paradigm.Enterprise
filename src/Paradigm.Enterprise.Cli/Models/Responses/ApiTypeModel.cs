using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;
internal sealed record ApiTypeModel([property: JsonPropertyName("name")] string Name, [property: JsonPropertyName("namespace")] string Namespace, [property: JsonPropertyName("baseType")] string? BaseType, [property: JsonPropertyName("interfaces")] IReadOnlyList<string> Interfaces, [property: JsonPropertyName("attributes")] IReadOnlyList<string> Attributes, [property: JsonPropertyName("genericConstraints")] IReadOnlyList<ApiGenericConstraint> GenericConstraints, [property: JsonPropertyName("members")] IReadOnlyList<ApiMember> Members);
