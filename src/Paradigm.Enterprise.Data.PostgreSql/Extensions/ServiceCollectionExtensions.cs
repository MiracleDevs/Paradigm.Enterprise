using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Data.PostgreSql.Context;

namespace Paradigm.Enterprise.Data.PostgreSql.Extensions;

/// <summary>
/// Provides dependency-injection registration for PostgreSQL Entity Framework contexts.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a scoped Entity Framework context configured to use a named PostgreSQL connection.
    /// </summary>
    /// <typeparam name="TContext">The <see cref="DbContext"/> implementation to register.</typeparam>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <param name="connectionStringName">The connection-string name resolved by <see cref="PostgreSqlDbConnectionProvider"/>.</param>
    /// <returns>The same service collection so that additional registrations can be chained.</returns>
    /// <remarks>The context uses the scoped connection supplied by <see cref="PostgreSqlDbConnectionProvider"/>.</remarks>
    public static IServiceCollection RegisterContext<TContext>(this IServiceCollection services, string connectionStringName) where TContext : DbContext
    {
        services.AddScoped(serviceProvider =>
        {
            var builder = new DbContextOptionsBuilder<TContext>();
            var connectionProvider = serviceProvider.GetRequiredService<PostgreSqlDbConnectionProvider>();
            var connection = connectionProvider[connectionStringName];
            builder.UseNpgsql(connection);
            return builder.Options;
        });

        return services.AddScoped<TContext>();
    }
}
