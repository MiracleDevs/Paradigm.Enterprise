using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Domain.Services;

namespace Paradigm.Enterprise.Domain.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the logged user service.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <returns></returns>
    public static IServiceCollection RegisterLoggedUserService(this IServiceCollection services)
    {
        return services.AddScoped<ILoggedUserService, LoggedUserService>();
    }
}
