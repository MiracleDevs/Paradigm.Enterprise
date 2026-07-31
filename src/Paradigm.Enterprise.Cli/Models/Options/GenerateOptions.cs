using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;

internal sealed record GenerateOptions(string Mode, string? ProjectName, string? AssemblyPath, string OutputPath, string? Document, string? SettingsPath, OutputFormat Format) : ICliCommandOptions
{
    #region Properties

    public string? Project => null;
    public string? Framework => null;

    #endregion
}