using EntityFramework.Exceptions.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Data.SqlServer.Context;

namespace Paradigm.Enterprise.Data.SqlServer.Extensions;

/// <summary>
/// Provides dependency-injection registration for SQL Server Entity Framework contexts.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a scoped Entity Framework context configured to use a named SQL Server connection.
    /// </summary>
    /// <typeparam name="TContext">The <see cref="DbContext"/> implementation to register.</typeparam>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <param name="connectionStringName">The connection-string name resolved by <see cref="SqlServerDbContextConnectionProvider"/>.</param>
    /// <returns>The same service collection so that additional registrations can be chained.</returns>
    /// <remarks>
    /// The context uses the scoped connection supplied by <see cref="SqlServerDbContextConnectionProvider"/>
    /// and enables translation of provider exceptions through EntityFramework.Exceptions.
    /// </remarks>
    public static IServiceCollection RegisterContext<TContext>(this IServiceCollection services, string connectionStringName) where TContext : DbContext
    {
        services.AddScoped(serviceProvider =>
        {
            var builder = new DbContextOptionsBuilder<TContext>();
            var connectionProvider = serviceProvider.GetRequiredService<SqlServerDbContextConnectionProvider>();
            var connection = connectionProvider[connectionStringName];
            builder.UseSqlServer(connection);
            builder.UseExceptionProcessor();
            return builder.Options;
        });

        return services.AddScoped<TContext>();
    }
}
