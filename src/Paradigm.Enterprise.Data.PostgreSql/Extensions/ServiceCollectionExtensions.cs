using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Data.PostgreSql.Context;

namespace Paradigm.Enterprise.Data.PostgreSql.Extensions;
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the context.
    /// </summary>
    /// <typeparam name="TContext">The type of the context.</typeparam>
    /// <param name="services">The services.</param>
    /// <param name="connectionStringName">Name of the connection string.</param>
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