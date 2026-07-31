using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;
internal sealed record ValidateOptions(string? Project, string? Framework, OutputFormat Format) : ICliCommandOptions;
