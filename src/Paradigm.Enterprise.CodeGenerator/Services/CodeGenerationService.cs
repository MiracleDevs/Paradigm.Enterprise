using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Paradigm.Enterprise.CodeGenerator.Generators;

namespace Paradigm.Enterprise.CodeGenerator;

internal static class CodeGenerationService
{
    #region Public Methods

    public static async Task<CodeGenerationResult> GenerateAsync(
        CodeGenerationRequest request,
        CancellationToken cancellationToken)
    {
        Validate(request);
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        var settings = request.SettingsPath ??
                       Path.Combine(AppContext.BaseDirectory, "paradigm-code-generator.settings.json");
        builder.Configuration.AddJsonFile(settings, optional: false, reloadOnChange: false);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ProjectName"] = request.ProjectName,
            ["ProvidersAssemblyPath"] = request.Mode == CodeGenerationMode.Json ? request.AssemblyPath : null,
            ["ProvidersOutputPath"] = request.Mode == CodeGenerationMode.Json ? request.OutputPath : null,
            ["DataAssemblyPath"] = request.Mode == CodeGenerationMode.Mappers ? request.AssemblyPath : null,
            ["DataOutputPath"] = request.Mode == CodeGenerationMode.Mappers ? request.OutputPath : null,
            ["ProxiesOutput"] = request.Mode == CodeGenerationMode.Client ? request.OutputPath : null,
            ["SwaggerUrl"] = request.Mode == CodeGenerationMode.Client ? request.Document : null
        });
        builder.Services.AddTransient<JsonContextGenerator>();
        builder.Services.AddTransient<StoredProcedureMapperGenerator>();
        builder.Services.AddTransient<ProxiesGenerator>();
        using var host = builder.Build();

        switch (request.Mode)
        {
            case CodeGenerationMode.Json:
                host.Services.GetRequiredService<JsonContextGenerator>().GenerateCode();
                break;
            case CodeGenerationMode.Mappers:
                host.Services.GetRequiredService<StoredProcedureMapperGenerator>().GenerateCode();
                break;
            case CodeGenerationMode.Client:
                await host.Services.GetRequiredService<ProxiesGenerator>().GenerateCodeAsync(cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request.Mode), request.Mode, null);
        }

        return new(request.Mode.ToString().ToLowerInvariant(), Path.GetFullPath(request.OutputPath));
    }

    #endregion

    #region Private Methods

    private static void Validate(CodeGenerationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OutputPath))
            throw new ArgumentException("Generation requires an output path.");
        if (request.Mode is CodeGenerationMode.Json or CodeGenerationMode.Mappers)
        {
            if (string.IsNullOrWhiteSpace(request.ProjectName))
                throw new ArgumentException("JSON and mapper generation require a project name.");
            if (string.IsNullOrWhiteSpace(request.AssemblyPath) || !File.Exists(request.AssemblyPath))
                throw new FileNotFoundException(
                    "Generation requires a built application assembly.",
                    request.AssemblyPath);
        }
        if (request.Mode == CodeGenerationMode.Client && string.IsNullOrWhiteSpace(request.Document))
            throw new ArgumentException("Client generation requires an OpenAPI document URL.");
        if (request.SettingsPath is not null && !File.Exists(request.SettingsPath))
            throw new FileNotFoundException("The generator settings file does not exist.", request.SettingsPath);
    }

    #endregion
}
