using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;
internal sealed record ApiShowOptions(string Symbol, string? Project, string? Framework, string? Package, OutputFormat Format) : ICliCommandOptions;
