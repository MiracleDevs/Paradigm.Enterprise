using Microsoft.Extensions.DependencyInjection;

namespace Paradigm.Enterprise.Providers;

/// <summary>
/// Provides access to the current dependency-injection scope for application-layer providers.
/// </summary>
/// <remarks>
/// The provider does not create or dispose the supplied <see cref="IServiceProvider"/>. Resolve provider
/// instances from a scope and allow that scope to manage their dependencies. <see cref="GetProvider{TProvider}"/>
/// is useful when one provider coordinates another, while constructor injection remains preferable for
/// dependencies that are always required.
/// </remarks>
/// <example>
/// A coordinating provider can resolve another provider from the same request or worker scope:
/// <code>
/// public interface IOrderProvider : IProvider
/// {
///     Task SubmitAsync(Guid orderId);
/// }
///
/// public sealed class CheckoutProvider : ProviderBase
/// {
///     public CheckoutProvider(IServiceProvider services) : base(services)
///     {
///     }
///
///     public Task CheckoutAsync(Guid orderId)
///     {
///         // IOrderProvider has the same scoped lifetime as this provider.
///         return GetProvider&lt;IOrderProvider&gt;().SubmitAsync(orderId);
///     }
/// }
///
/// services.AddScoped&lt;IOrderProvider, OrderProvider&gt;();
/// services.AddScoped&lt;CheckoutProvider&gt;();
///
/// await using AsyncServiceScope scope = rootProvider.CreateAsyncScope();
/// var checkout = scope.ServiceProvider.GetRequiredService&lt;CheckoutProvider&gt;();
/// await checkout.CheckoutAsync(orderId);
/// // Disposing the scope disposes scoped repositories and contexts; CheckoutProvider does not.
/// </code>
/// </example>
public abstract class ProviderBase : IProvider
{
    #region Properties

    /// <summary>
    /// Gets the service provider for the scope in which this provider was created.
    /// </summary>
    /// <value>
    /// The externally owned service provider used to resolve collaborators.
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
    /// Resolves another provider from the current service scope.
    /// </summary>
    /// <typeparam name="TProvider">The type of the provider.</typeparam>
    /// <returns>The provider resolved from the current service scope.</returns>
    /// <exception cref="InvalidOperationException">
    /// No service for <typeparamref name="TProvider"/> is registered.
    /// </exception>
    /// <remarks>
    /// The dependency-injection container controls the resolved provider's lifetime. This method does not
    /// create a child scope and callers must not dispose the returned instance independently.
    /// </remarks>
    protected virtual TProvider GetProvider<TProvider>() where TProvider : IProvider
    {
        return ServiceProvider.GetRequiredService<TProvider>();
    }

    #endregion
}
