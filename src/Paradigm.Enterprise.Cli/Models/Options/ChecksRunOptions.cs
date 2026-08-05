using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;

internal sealed record ChecksRunOptions(string? Project, string? Framework, string? Config, OutputFormat Format) : ICliCommandOptions;