using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Domain.Services;

namespace Paradigm.Enterprise.Domain.Extensions;

/// <summary>
/// Adds domain-layer services to an application's dependency-injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the scoped service that stores the authenticated user for the current operation.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The same service collection so that registrations can be chained.</returns>
    /// <example>
    /// Register once during application startup, then set and read the current user within a service scope:
    /// <code>
    /// services.RegisterLoggedUserService();
    ///
    /// await using AsyncServiceScope scope = rootProvider.CreateAsyncScope();
    /// var loggedUsers = scope.ServiceProvider.GetRequiredService&lt;ILoggedUserService&lt;Guid&gt;&gt;();
    ///
    /// loggedUsers.Authenticate(currentUser);
    /// User requiredUser = loggedUsers.GetAuthenticatedUser&lt;User&gt;();
    ///
    /// loggedUsers.Authenticate&lt;User&gt;(null); // Clears authentication in this scope.
    /// </code>
    /// A different dependency-injection scope receives an independent logged-user service.
    /// </example>
    public static IServiceCollection RegisterLoggedUserService(this IServiceCollection services)
    {
        return services.AddScoped(typeof(ILoggedUserService<>), typeof(LoggedUserService<>));
    }
}
