using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Data.MySql.Context;
using Paradigm.Enterprise.Data.MySql.Extensions;
using System.Data;

namespace Paradigm.Enterprise.Data.MySql.Tests;

[TestClass]
public sealed class ConnectionRegistrationTests
{
    #region Nested Types

    public sealed class FirstContext(DbContextOptions<FirstContext> options) : DbContext(options);

    public sealed class SecondContext(DbContextOptions<SecondContext> options) : DbContext(options);

    #endregion

    #region Public Methods

    [TestMethod]
    public void Contexts_share_one_named_connection_without_opening_it_and_scopes_are_isolated()
    {
        using var services = CreateServices();
        using var firstScope = services.CreateScope();
        using var secondScope = services.CreateScope();
        var first = firstScope.ServiceProvider.GetRequiredService<FirstContext>();
        var second = firstScope.ServiceProvider.GetRequiredService<SecondContext>();
        var connections = firstScope.ServiceProvider.GetRequiredService<MySqlDbConnectionProvider>();
        var connection = connections["Catalog"];

        Assert.AreSame(connection, first.Database.GetDbConnection());
        Assert.AreSame(connection, second.Database.GetDbConnection());
        Assert.AreSame(connection, connections["Catalog"]);
        Assert.AreNotSame(connection, connections["Other"]);
        Assert.AreNotSame(connection, secondScope.ServiceProvider.GetRequiredService<FirstContext>().Database.GetDbConnection());
        Assert.AreEqual(ConnectionState.Closed, connection.State);
        Assert.AreEqual("MySql.EntityFrameworkCore", first.Database.ProviderName);

        first.Dispose();
        Assert.AreEqual("synthetic", connection.Database);
        Assert.AreSame(connection, second.Database.GetDbConnection());
    }

    [TestMethod]
    public void Missing_connection_configuration_fails_with_key_without_credentials()
    {
        using var services = CreateServices();
        using var scope = services.CreateScope();
        var connections = scope.ServiceProvider.GetRequiredService<MySqlDbConnectionProvider>();

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => _ = connections["Missing"]);

        StringAssert.Contains(exception.Message, "Missing");
        Assert.IsFalse(exception.Message.Contains("synthetic-password", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Blank_connection_configuration_fails_before_creating_a_connection()
    {
        using var connections = new MySqlDbConnectionProvider(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Blank"] = " " })
            .Build());

        Assert.ThrowsExactly<InvalidOperationException>(() => _ = connections["Blank"]);
    }

    [TestMethod]
    public void Disposed_provider_does_not_create_or_return_connections()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Catalog"] = "Server=localhost;Database=synthetic" })
            .Build();
        var provider = new MySqlDbConnectionProvider(configuration);
        _ = provider["Catalog"];
        provider.Dispose();
        provider.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => _ = provider["Catalog"]);
    }

    #endregion

    #region Private Methods

    private static ServiceProvider CreateServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Catalog"] = "Server=127.0.0.1;Port=1;Database=synthetic;User ID=synthetic;Password=synthetic-password",
                ["ConnectionStrings:Other"] = "Server=127.0.0.1;Port=1;Database=other"
            }).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddScoped<MySqlDbConnectionProvider>();
        services.RegisterContext<FirstContext>("Catalog");
        services.RegisterContext<SecondContext>("Catalog");
        return services.BuildServiceProvider();
    }

    #endregion
}