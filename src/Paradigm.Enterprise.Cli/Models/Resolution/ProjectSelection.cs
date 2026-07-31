using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;

internal sealed record ProjectSelection(IReadOnlyList<string> Projects, string DisplayPath);