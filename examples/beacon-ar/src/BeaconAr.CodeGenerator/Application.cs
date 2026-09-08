using BeaconAr.CodeGenerator.Generators;
using Microsoft.Extensions.DependencyInjection;

namespace BeaconAr.CodeGenerator;

internal class Application
{
    #region Properties

    /// <summary>
    /// The service provider
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="Application"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public Application(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Executes this instance.
    /// </summary>
    public async Task ExecuteAsync(IReadOnlyCollection<string> arguments)
    {
        var targets = GenerationTargets.Parse(arguments);
        if (targets.Contains(GenerationTarget.JsonContexts))
            _serviceProvider.GetRequiredService<JsonContextGenerator>().GenerateCode();
        if (targets.Contains(GenerationTarget.StoredProcedureMappers))
            _serviceProvider.GetRequiredService<StoredProcedureMapperGenerator>().GenerateCode();

        await Task.CompletedTask;
    }

    #endregion
}
