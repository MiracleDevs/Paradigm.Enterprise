using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;
internal sealed record InspectOptions(string? Project, string? Framework, OutputFormat Format) : ICliCommandOptions;
