using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;
internal sealed record ApiSearchOptions(string Query, string? Project, string? Framework, string? Package, int Limit, OutputFormat Format) : ICliCommandOptions;
