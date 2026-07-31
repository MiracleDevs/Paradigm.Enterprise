using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;

internal sealed record InspectedType(string FullName, string Name, string Namespace, string? BaseType, IReadOnlyList<string> Interfaces, IReadOnlyList<string> Attributes, IReadOnlyList<string> Members, IReadOnlyList<InspectedAction> Actions, bool IsPublic, bool IsAbstract, string AssemblyName, string? Package, string? Version, IReadOnlyList<ApiMember>? StructuredMembers = null, IReadOnlyList<ApiGenericConstraint>? GenericConstraints = null);