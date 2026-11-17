using Paradigm.Enterprise.CodeGenerator;

namespace Paradigm.Enterprise.Cli;

internal sealed record GenerateOptions(string Mode, string? ProjectName, string? AssemblyPath, string OutputPath, string? Document, string? SettingsPath, OutputFormat Format, CodeGenerationDatabaseProvider DatabaseProvider = CodeGenerationDatabaseProvider.SqlServer) : ICliCommandOptions
{
    #region Properties

    public string? Project => null;
    public string? Framework => null;

    #endregion
}