using NUnit.Framework;

namespace Paradigm.Enterprise.Aspire.DatabaseBootstrap.SqlServer.Tests;

/// <summary>
/// Tests SQL Server bootstrap option defaults.
/// </summary>
[TestFixture]
public sealed class SqlServerDatabaseBootstrapOptionsTests
{
    /// <summary>
    /// Verifies the default finite-bootstrap behavior.
    /// </summary>
    [Test]
    public void Defaults_use_a_finite_database_bootstrap_contract()
    {
        var options = new SqlServerDatabaseBootstrapOptions
        {
            DockerBuildContext = "../../../",
            DockerfilePath = "docker/DatabaseBootstrap/Dockerfile",
            DatabaseName = "demo",
            SqlServerHost = "sqlserver"
        };

        Assert.Multiple(() =>
        {
            Assert.That(options.ResourceName, Is.EqualTo("database-bootstrap"));
            Assert.That(options.PublishOnStart, Is.False);
        });
    }
}
