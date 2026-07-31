using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;

internal sealed record PackagesCheckOptions(string? Project, string? Framework, string? Config, OutputFormat Format) : ICliCommandOptions;