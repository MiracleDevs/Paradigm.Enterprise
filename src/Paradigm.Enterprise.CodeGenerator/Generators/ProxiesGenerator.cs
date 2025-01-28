using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSwag;
using NSwag.CodeGeneration.OperationNameGenerators;
using NSwag.CodeGeneration.TypeScript;

namespace Paradigm.Enterprise.CodeGenerator.Generators;

internal class ProxiesGenerator
{
    #region Properties

    /// <summary>
    /// The configuration
    /// </summary>
    private readonly IConfiguration _configuration;

    /// <summary>
    /// The logger
    /// </summary>
    private readonly ILogger _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ProxiesGenerator" /> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="logger">The logger.</param>
    public ProxiesGenerator(IConfiguration configuration, ILogger<ProxiesGenerator> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Generates the code.
    /// </summary>
    public async Task GenerateCodeAsync()
    {
        try
        {
            var proxiesGeneratorConfiguration = _configuration.GetRequiredSection("proxiesGenerator");

            var swaggerUrl = _configuration.GetValue<string>("SwaggerUrl") ?? proxiesGeneratorConfiguration.GetValue<string?>("swaggerUrl");
            if (string.IsNullOrEmpty(swaggerUrl))
                throw new ArgumentNullException(nameof(swaggerUrl));

            var outputFile = proxiesGeneratorConfiguration.GetValue<string?>("outputFile");
            if (string.IsNullOrEmpty(outputFile))
                throw new ArgumentNullException(nameof(outputFile));

            var settings = proxiesGeneratorConfiguration.GetRequiredSection("typeScriptClient").Get<TypeScriptClientGeneratorSettings>()
                ?? throw new ArgumentNullException("typeScriptClient");

            var outputPath = _configuration.GetValue<string>("ProxiesOutput");
            if (string.IsNullOrEmpty(outputPath))
                throw new ArgumentNullException("ProxiesOutput");

            outputFile = Path.Combine(outputPath, outputFile);
            settings.TypeScriptGeneratorSettings.ExtensionCode = Path.Combine(outputPath, settings.TypeScriptGeneratorSettings.ExtensionCode);

            _logger.LogInformation("Starting API proxies generation.");

            if (!string.IsNullOrWhiteSpace(settings.TypeScriptGeneratorSettings.ExtensionCode))
            {
                var extensionCodeContent = File.ReadAllText(settings.TypeScriptGeneratorSettings.ExtensionCode);
                settings.TypeScriptGeneratorSettings.ExtensionCode = extensionCodeContent;
            }

            settings.OperationNameGenerator = new MultipleClientsFromFirstTagAndOperationIdGenerator();

            _logger.LogInformation($"Generating '{outputFile}' from '{swaggerUrl}'...");

            var document = await OpenApiDocument.FromUrlAsync(swaggerUrl);
            var code = new TypeScriptClientGenerator(document, settings).GenerateFile();
            await File.WriteAllTextAsync(outputFile, code);

            _logger.LogInformation("Proxies generated.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
        finally
        {
            _logger.LogInformation("Finished API proxies generation.");
            _logger.LogInformation("---------------------------------------------");
        }
    }

    #endregion
}