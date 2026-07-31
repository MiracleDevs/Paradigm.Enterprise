using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;

internal sealed record CommandResponse([property: JsonPropertyName("schemaVersion")] string SchemaVersion, [property: JsonPropertyName("command")] string Command, [property: JsonPropertyName("status")] string Status, [property: JsonPropertyName("packages")] IReadOnlyList<PackageInfo> Packages, [property: JsonPropertyName("results")] IReadOnlyList<ResultItem> Results, [property: JsonPropertyName("diagnostics")] IReadOnlyList<Diagnostic> Diagnostics, [property: JsonPropertyName("guide")] GuideInfo? Guide = null, [property: JsonPropertyName("checks")] CheckData? Checks = null);