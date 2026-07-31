using System.Text.Json;
using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;

internal sealed record PackagePolicyConfiguration([property: JsonPropertyName("includePrerelease")] bool IncludePrerelease = false);