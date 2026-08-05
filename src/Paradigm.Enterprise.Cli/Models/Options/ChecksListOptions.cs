using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;

internal sealed record ChecksListOptions(OutputFormat Format) : ICliCommandOptions
{
    #region Properties

    public string? Project => null;

    public string? Framework => null;

    #endregion
}