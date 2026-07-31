using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;

internal sealed record PackagesAuditOptions(string? Project, string? Framework, string? Config, bool WarningsAsErrors, OutputFormat Format) : ICliCommandOptions;