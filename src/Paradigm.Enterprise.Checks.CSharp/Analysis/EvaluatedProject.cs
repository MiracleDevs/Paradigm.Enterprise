using System.Diagnostics;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Paradigm.Enterprise.Checks.CSharp;

internal sealed record EvaluatedProject(
    IReadOnlyList<string> SourceFiles,
    IReadOnlyList<string> ReferencePaths,
    IReadOnlyList<string> AnalyzerPaths,
    LanguageVersion LanguageVersion,
    IReadOnlyList<string> DefineConstants,
    IReadOnlyList<string> InterceptorNamespaces,
    NullableContextOptions NullableContextOptions,
    OutputKind OutputKind,
    bool AllowUnsafe,
    bool IsTestProject)
{
    #region Public Methods

    public CSharpParseOptions CreateParseOptions() => new CSharpParseOptions(LanguageVersion,
            preprocessorSymbols: DefineConstants)
        .WithFeatures(InterceptorNamespaces.Count == 0
            ? []
            : [new("InterceptorsNamespaces", string.Join(';', InterceptorNamespaces))]);

    public static EvaluatedProject Load(string project, string framework)
    {
        var targetFramework = framework.Split('/')[0];
        using var process = new Process
        {
            StartInfo = new()
            {
                FileName = "dotnet",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8,
                CreateNoWindow = true,
                ArgumentList =
                {
                    "msbuild",
                    project,
                    "-nologo",
                    "-target:ResolveReferences",
                    "-getItem:Compile,ReferencePath,Analyzer",
                    "-getProperty:DefineConstants,LangVersion,Nullable,OutputType,AllowUnsafeBlocks,InterceptorsNamespaces,InterceptorsPreviewNamespaces,IsTestProject",
                    "-property:Configuration=Release",
                    $"-property:TargetFramework={targetFramework}"
                }
            }
        };
        process.Start();
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"Could not evaluate Compile items for '{project}': {error.GetAwaiter().GetResult()}");

        using var document = JsonDocument.Parse(output.GetAwaiter().GetResult());
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
        var interceptorNamespaces = PropertyValues(properties, "InterceptorsNamespaces")
            .Concat(PropertyValues(properties, "InterceptorsPreviewNamespaces"))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
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
        var isTestProject = bool.TryParse(
            properties.GetProperty("IsTestProject").GetString(), out var testValue) && testValue;
        return new(files, references, analyzers, language, constants, interceptorNamespaces, nullable, outputKind, allowUnsafe, isTestProject);
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

    private static string[] PropertyValues(JsonElement properties, string name) =>
        properties.GetProperty(name).GetString()?
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

    #endregion
}
