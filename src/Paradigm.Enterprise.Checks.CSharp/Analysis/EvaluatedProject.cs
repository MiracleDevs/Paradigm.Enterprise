using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Text.Json;

namespace Paradigm.Enterprise.Checks.CSharp;

internal sealed record EvaluatedProject(
    IReadOnlyList<string> SourceFiles,
    IReadOnlyList<string> ReferencePaths,
    IReadOnlyList<string> AnalyzerPaths,
    LanguageVersion LanguageVersion,
    IReadOnlyList<string> DefineConstants,
    NullableContextOptions NullableContextOptions,
    OutputKind OutputKind,
    bool AllowUnsafe)
{
    #region Public Methods

    public static EvaluatedProject Load(string project, string framework)
    {
        var targetFramework = framework.Split('/')[0];
        var output = MsBuildProcess.Run([
            project,
            "-target:ResolveReferences",
            "-getItem:Compile,ReferencePath,Analyzer",
            "-getProperty:DefineConstants,LangVersion,Nullable,OutputType,AllowUnsafeBlocks",
            "-property:Configuration=Release",
            $"-property:TargetFramework={targetFramework}"
        ]);
        using var document = JsonDocument.Parse(output);
        var root = document.RootElement;
        var properties = root.GetProperty("Properties");
        var files = Items(root, "Compile");
        var references = Items(root, "ReferencePath");
        var analyzers = Items(root, "Analyzer");
        if (files.Length == 0 || references.Length == 0)
            throw new InvalidOperationException(
                $"MSBuild evaluation for '{project}' returned {files.Length} Compile item(s) and " +
                $"{references.Length} ReferencePath item(s); semantic analysis requires both.");
        var languageValue = properties.GetProperty("LangVersion").GetString();
        var language = languageValue is not null &&
                       LanguageVersionFacts.TryParse(languageValue, out var parsedLanguage)
            ? parsedLanguage
            : LanguageVersion.Latest;
        var constants = properties.GetProperty("DefineConstants").GetString()?
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
        var nullable = properties.GetProperty("Nullable").GetString()?.ToLowerInvariant() switch
        {
            "enable" => NullableContextOptions.Enable,
            "annotations" => NullableContextOptions.Annotations,
            "warnings" => NullableContextOptions.Warnings,
            _ => NullableContextOptions.Disable
        };
        var outputKind = properties.GetProperty("OutputType").GetString() is "Exe" or "WinExe"
            ? OutputKind.ConsoleApplication
            : OutputKind.DynamicallyLinkedLibrary;
        var allowUnsafe = bool.TryParse(
            properties.GetProperty("AllowUnsafeBlocks").GetString(), out var unsafeValue) && unsafeValue;
        return new(files, references, analyzers, language, constants, nullable, outputKind, allowUnsafe);
    }

    #endregion

    #region Private Methods

    private static string[] Items(JsonElement root, string name) =>
        root.GetProperty("Items").GetProperty(name).EnumerateArray()
            .Select(item => item.GetProperty("FullPath").GetString())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFullPath(path!))
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    #endregion
}
