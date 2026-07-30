using Microsoft.Extensions.DependencyInjection;

namespace Paradigm.Enterprise.Providers;

/// <summary>
/// Provides dependency-injection access shared by application-layer providers.
/// </summary>
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
    /// <returns>The provider resolved from the current service scope.</returns>
    protected virtual TProvider GetProvider<TProvider>() where TProvider : IProvider
    {
        return ServiceProvider.GetRequiredService<TProvider>();
    }

    #endregion
}
