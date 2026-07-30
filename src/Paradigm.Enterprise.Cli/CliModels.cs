using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;

internal enum OutputFormat
{
    Text,
    Json
}

internal sealed record ParsedCommand(
    string Name,
    string? Query,
    string? Project,
    string? Framework,
    string? Package,
    int Limit,
    OutputFormat Format);

internal sealed record Diagnostic(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("severity")] string Severity,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("location")] string? Location = null);

internal sealed record PackageInfo(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("project")] string Project);

internal sealed record ResultItem(
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("detail")] string Detail,
    [property: JsonPropertyName("package")] string? Package = null,
    [property: JsonPropertyName("version")] string? Version = null,
    [property: JsonPropertyName("project")] string? Project = null);

internal sealed record CommandResponse(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("command")] string Command,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("packages")] IReadOnlyList<PackageInfo> Packages,
    [property: JsonPropertyName("results")] IReadOnlyList<ResultItem> Results,
    [property: JsonPropertyName("diagnostics")] IReadOnlyList<Diagnostic> Diagnostics);

internal sealed record ProjectSelection(IReadOnlyList<string> Projects, string DisplayPath);

internal sealed record AssetSelection(
    string Project,
    string Framework,
    IReadOnlyList<PackageInfo> Packages,
    IReadOnlyList<string> MetadataPaths,
    IReadOnlyDictionary<string, (string Name, string Version)> AssemblyOwners,
    IReadOnlySet<string> InspectionAssemblyNames,
    string? ApplicationAssembly,
    string? ExpectedApplicationAssembly,
    bool ApplicationAssemblyIsStale,
    IReadOnlyList<Diagnostic> Diagnostics);

internal sealed record InspectedAction(
    string Name,
    string Signature,
    IReadOnlyList<string> Attributes);

internal sealed record InspectedType(
    string FullName,
    string Name,
    string Namespace,
    string? BaseType,
    IReadOnlyList<string> Interfaces,
    IReadOnlyList<string> Attributes,
    IReadOnlyList<string> Members,
    IReadOnlyList<InspectedAction> Actions,
    bool IsPublic,
    bool IsAbstract,
    string AssemblyName,
    string? Package,
    string? Version);
