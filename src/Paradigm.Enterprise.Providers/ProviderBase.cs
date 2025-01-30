using Microsoft.Extensions.DependencyInjection;

namespace Paradigm.Enterprise.Providers;

public abstract class ProviderBase : IProvider
{
    #region Properties

    /// <summary>
    /// Gets the service provider.
    /// </summary>
    /// <value>
    /// The service provider.
    /// </value>
    protected IServiceProvider ServiceProvider { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderBase"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    protected ProviderBase(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Gets the provider.
    /// </summary>
    /// <typeparam name="TProvider">The type of the provider.</typeparam>
    /// <returns></returns>
    protected virtual TProvider GetProvider<TProvider>() where TProvider : IProvider
    {
        return ServiceProvider.GetRequiredService<TProvider>();
    }

    #endregion
}