using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;

internal sealed record InspectedAction(string Name, string Signature, IReadOnlyList<string> Attributes);