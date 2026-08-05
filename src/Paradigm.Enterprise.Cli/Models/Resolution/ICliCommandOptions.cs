using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;

internal interface ICliCommandOptions
{
    string? Project { get; }

    string? Framework { get; }

    OutputFormat Format { get; }
}