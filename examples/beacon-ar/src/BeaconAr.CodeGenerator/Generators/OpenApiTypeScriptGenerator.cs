using NJsonSchema.CodeGeneration.TypeScript;
using NSwag;
using NSwag.CodeGeneration.TypeScript;

namespace BeaconAr.CodeGenerator.Generators;

internal static class OpenApiTypeScriptGenerator
{
    #region Public Methods

    public static async Task GenerateAsync(string documentPath, string outputPath)
    {
        string source = Path.GetFullPath(documentPath);
        string target = Path.GetFullPath(outputPath);
        if (!File.Exists(source))
            throw new FileNotFoundException("The OpenAPI document was not found.", source);

        OpenApiDocument document = await OpenApiDocument.FromFileAsync(source);
        TypeScriptClientGeneratorSettings settings = new()
        {
            ClassName = "{controller}Client",
            Template = TypeScriptTemplate.Angular,
            GenerateClientClasses = true,
            GenerateClientInterfaces = true,
            InjectionTokenType = InjectionTokenType.InjectionToken,
            RxJsVersion = 7.0m,
        };
        settings.TypeScriptGeneratorSettings.TypeScriptVersion = 5.0m;
        settings.TypeScriptGeneratorSettings.TypeStyle = TypeScriptTypeStyle.Interface;
        settings.TypeScriptGeneratorSettings.NullValue = TypeScriptNullValue.Null;
        string generated = new TypeScriptClientGenerator(document, settings).GenerateFile();
        Directory.CreateDirectory(Path.GetDirectoryName(target) ?? throw new InvalidOperationException("The output path requires a directory."));
        await File.WriteAllTextAsync(target, generated);
    }

    #endregion
}
