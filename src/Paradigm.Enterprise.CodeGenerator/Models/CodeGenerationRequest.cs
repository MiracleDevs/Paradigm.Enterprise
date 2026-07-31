namespace Paradigm.Enterprise.CodeGenerator;

internal sealed record CodeGenerationRequest(
    CodeGenerationMode Mode,
    string? ProjectName,
    string? AssemblyPath,
    string OutputPath,
    string? Document,
    string? SettingsPath);
