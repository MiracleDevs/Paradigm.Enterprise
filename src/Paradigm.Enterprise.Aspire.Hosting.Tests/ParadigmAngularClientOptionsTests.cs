using NUnit.Framework;

namespace Paradigm.Enterprise.Aspire.Hosting.Tests;

/// <summary>
/// Tests Angular client hosting defaults.
/// </summary>
[TestFixture]
public sealed class ParadigmAngularClientOptionsTests
{
    /// <summary>
    /// Verifies the default client composition behavior.
    /// </summary>
    [Test]
    public void Defaults_support_dynamic_host_ports_and_external_browser_access()
    {
        var options = new ParadigmAngularClientOptions
        {
            Name = "client",
            WorkingDirectory = "../../client"
        };

        Assert.Multiple(() =>
        {
            Assert.That(options.HostPort, Is.Null);
            Assert.That(options.TargetPort, Is.EqualTo(4200));
            Assert.That(options.RunScript, Is.EqualTo("start"));
            Assert.That(options.ExposeExternalEndpoints, Is.True);
            Assert.That(options.WaitForApis, Is.False);
        });
    }
}
