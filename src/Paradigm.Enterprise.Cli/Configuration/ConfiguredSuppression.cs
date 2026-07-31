using System.Text.Json;
using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;
internal sealed record ConfiguredSuppression([property: JsonPropertyName("code")] string Code, [property: JsonPropertyName("symbol")] string? Symbol, [property: JsonPropertyName("location")] string? Location, [property: JsonPropertyName("reason")] string Reason, [property: JsonPropertyName("expires")] DateOnly Expires);
