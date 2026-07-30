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
    /// <code>
    /// services.RegisterLoggedUserService();
    /// </code>
    /// </example>
    public static IServiceCollection RegisterLoggedUserService(this IServiceCollection services)
    {
        return services.AddScoped(typeof(ILoggedUserService<>), typeof(LoggedUserService<>));
    }
}
