using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;
internal sealed record VersionOptions(OutputFormat Format = OutputFormat.Text) : ICliCommandOptions
{
#region Properties
    public string? Project => null;
    public string? Framework => null;
#endregion
}
