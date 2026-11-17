using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Data.MySql.Context;

namespace Paradigm.Enterprise.Data.MySql.Extensions;

/// <summary>Registers scoped Entity Framework contexts using Oracle's MySQL provider.</summary>
public static class ServiceCollectionExtensions
{
    #region Public Methods

    /// <summary>Registers a context using a provider-owned, named MySQL connection.</summary>
    /// <typeparam name="TContext">The application-owned Entity Framework context.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionStringName">The name of a configured connection string.</param>
    /// <returns>The supplied collection for chaining.</returns>
    /// <remarks>
    /// Register <see cref="MySqlDbConnectionProvider"/> as scoped before resolving the context.
    /// Registration does not connect to the server. EF borrows the connection without owning it.
    /// </remarks>
    public static IServiceCollection RegisterContext<TContext>(this IServiceCollection services, string connectionStringName)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringName);

        services.AddScoped(serviceProvider =>
        {
            var connectionProvider = serviceProvider.GetRequiredService<MySqlDbConnectionProvider>();
            var builder = new DbContextOptionsBuilder<TContext>();
            builder.UseMySQL(connectionProvider[connectionStringName]);
            return builder.Options;
        });

        return services.AddScoped<TContext>();
    }

    #endregion
}