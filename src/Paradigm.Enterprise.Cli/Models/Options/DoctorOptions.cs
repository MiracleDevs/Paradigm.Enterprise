using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;

internal sealed record DoctorOptions(string? Project, OutputFormat Format) : ICliCommandOptions
{
    #region Properties

    public string? Framework => null;

    #endregion
}