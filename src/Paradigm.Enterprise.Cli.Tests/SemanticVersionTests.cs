namespace Paradigm.Enterprise.Cli.Tests;

[TestClass]
public class SemanticVersionTests
{
    [TestMethod]
    [DataRow("1.0.0-alpha", "1.0.0-alpha.1")]
    [DataRow("1.0.0-alpha.2", "1.0.0-alpha.10")]
    [DataRow("1.0.0-alpha.10", "1.0.0-beta")]
    [DataRow("1.0.0-beta.11", "1.0.0-rc.1")]
    [DataRow("1.0.0-rc.1", "1.0.0")]
    [DataRow("1.9.9", "2.0.0")]
    public void Semantic_version_precedence_is_respected(string lower, string higher)
    {
        Assert.IsTrue(PackagePolicyService.CompareVersions(lower, higher) < 0);
        Assert.IsTrue(PackagePolicyService.CompareVersions(higher, lower) > 0);
    }

    [TestMethod]
    public void Build_metadata_does_not_change_version_precedence_or_alignment()
    {
        Assert.AreEqual(0,
            PackagePolicyService.CompareVersions("1.1.0+build.1", "1.1.0+build.2"));
        Assert.IsEmpty(ResponseFactory.VersionDiagnostics(
            [
                new("Paradigm.Enterprise.Domain", "1.1.0+build.1", "One.csproj"),
                new("Paradigm.Enterprise.Data", "1.1.0+build.2", "Two.csproj")
            ]));
    }

    [TestMethod]
    public void Prerelease_framework_does_not_align_with_stable_cli()
    {
        var configuration = new ParadigmConfiguration("config.json", ".", [], new(), []);

        var diagnostics = new PackagePolicyService().Check(
            [new("Paradigm.Enterprise.Domain", "1.1.0-rc.1", "App.csproj")],
            [],
            configuration);

        Assert.IsTrue(diagnostics.Any(x => x.Code == "PE7002"));
    }

    [TestMethod]
    public void Prerelease_package_is_below_the_corresponding_stable_minimum()
    {
        Assert.IsTrue(PackagePolicyService.CompareVersions("2.0.0-rc.1", "2.0.0") < 0);
    }

    [TestMethod]
    [DataRow("1.0")]
    [DataRow("1.0.0-01")]
    [DataRow("1.0.0+")]
    public void Invalid_semantic_versions_are_rejected(string value)
    {
        Assert.IsFalse(SemanticVersion.TryParse(value, out _));
    }
}
