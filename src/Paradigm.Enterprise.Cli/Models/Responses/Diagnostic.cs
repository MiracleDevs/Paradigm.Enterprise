using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;

internal sealed record Diagnostic([property: JsonPropertyName("code")] string Code, [property: JsonPropertyName("severity")] string Severity, [property: JsonPropertyName("message")] string Message, [property: JsonPropertyName("location")] string? Location = null);