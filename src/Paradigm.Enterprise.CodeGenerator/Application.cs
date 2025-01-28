using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.CodeGenerator.Generators;

namespace Microsoft.DemoManagementSystem.WebApi.CodeGenerator;
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
    public async Task ExecuteAsync()
    {
        _serviceProvider.GetRequiredService<JsonContextGenerator>().GenerateCode();
        _serviceProvider.GetRequiredService<StoredProcedureMapperGenerator>().GenerateCode();
        await _serviceProvider.GetRequiredService<ProxiesGenerator>().GenerateCodeAsync();
    }

    #endregion
}