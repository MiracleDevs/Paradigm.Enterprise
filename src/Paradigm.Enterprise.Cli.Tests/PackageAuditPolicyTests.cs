using System.Text.Json;

namespace Paradigm.Enterprise.Cli.Tests;

[TestClass]
public class PackageAuditPolicyTests
{
    #region Nested Types

    private sealed class AuditFixture : IDisposable
    {
        #region Properties

        public string Root { get; }
        public string Executable { get; }

        #endregion

        #region Constructors

        private AuditFixture(string root, string executable)
        {
            Root = root;
            Executable = executable;
        }

        #endregion

        #region Public Methods

        public static AuditFixture Create()
        {
            var root = Path.Combine(Path.GetTempPath(), $"paradigm-audit-fixture-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            var repository = FindRepositoryRoot();
            var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
            var source = Path.Combine(repository, "src", "Paradigm.Enterprise.Cli.Tests", "Fixtures", "DotNetAuditFixture", "bin", configuration, "net10.0");
            foreach (var file in Directory.EnumerateFiles(source))
                File.Copy(file, Path.Combine(root, Path.GetFileName(file)));
            var executable = Path.Combine(root, OperatingSystem.IsWindows() ? "DotNetAuditFixture.exe" : "DotNetAuditFixture");
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(executable, File.GetUnixFileMode(executable) | UnixFileMode.UserExecute);
            return new(root, executable);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }

        #endregion
    }

    #endregion

    #region Public Methods

    [TestMethod]
    public void Vulnerable_deprecated_and_outdated_policies_are_classified()
    {
        var json = AuditJson("""
            {"id":"Direct.Stable","resolvedVersion":"1.0.0","latestVersion":"2.0.0",
             "vulnerabilities":[{"severity":"High"}],"deprecationReasons":["Legacy"]},
            {"id":"Direct.Prerelease","resolvedVersion":"1.0.0-rc.1","latestVersion":"2.0.0"}
            """, """
            {"id":"Transitive.Stable","resolvedVersion":"1.0.0","latestVersion":"2.0.0",
             "vulnerabilities":[{"severity":"Moderate"}],"deprecationAlternative":"Replacement"}
            """);
        var vulnerable = DotNetPackageAuditor.ReadFindings(json, "--vulnerable", "App.csproj", includePrerelease: false);
        var deprecated = DotNetPackageAuditor.ReadFindings(json, "--deprecated", "App.csproj", includePrerelease: false);
        var outdated = DotNetPackageAuditor.ReadFindings(json, "--outdated", "App.csproj", includePrerelease: false);
        CollectionAssert.AreEquivalent(new[] { "Direct.Stable", "Transitive.Stable" }, vulnerable.Select(Name).ToArray());
        Assert.IsTrue(vulnerable.All(x => x.Code == "PE7006" && x.Severity == "error"));
        CollectionAssert.AreEquivalent(new[] { "Direct.Stable", "Transitive.Stable" }, deprecated.Select(Name).ToArray());
        Assert.IsTrue(deprecated.All(x => x.Code == "PE7007" && x.Severity == "warning"));
        CollectionAssert.AreEqual(new[] { "Direct.Stable" }, outdated.Select(Name).ToArray());
        Assert.AreEqual("PE7008", outdated.Single().Code);
    }

    [TestMethod]
    public void Prerelease_findings_are_included_only_when_configured()
    {
        var json = AuditJson("""
            {"id":"Preview.Package","resolvedVersion":"1.0.0-rc.1","latestVersion":"1.0.0"}
            """, "");
        Assert.IsEmpty(DotNetPackageAuditor.ReadFindings(json, "--outdated", "App.csproj", includePrerelease: false));
        Assert.HasCount(1, DotNetPackageAuditor.ReadFindings(json, "--outdated", "App.csproj", includePrerelease: true));
    }

    [TestMethod]
    public async Task Private_feed_failure_is_an_incomplete_audit_error()
    {
        using var fixture = AuditFixture.Create();
        var project = Path.Combine(fixture.Root, "AuditFailure.csproj");
        await File.WriteAllTextAsync(project, "<Project />");
        var configuration = new ParadigmConfiguration("config.json", fixture.Root, new(), []);
        var diagnostics = await new DotNetPackageAuditor(fixture.Executable).AuditAsync(new([project], project), configuration, CancellationToken.None);
        Assert.HasCount(3, diagnostics);
        Assert.IsTrue(diagnostics.All(x => x.Code == "PE7005" && x.Severity == "error" && x.Message.Contains("private feed unavailable", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task Malformed_audit_json_is_an_incomplete_audit_error()
    {
        using var fixture = AuditFixture.Create();
        var project = Path.Combine(fixture.Root, "AuditMalformed.csproj");
        await File.WriteAllTextAsync(project, "<Project />");
        var configuration = new ParadigmConfiguration("config.json", fixture.Root, new(), []);
        var diagnostics = await new DotNetPackageAuditor(fixture.Executable).AuditAsync(new([project], project), configuration, CancellationToken.None);
        Assert.HasCount(3, diagnostics);
        Assert.IsTrue(diagnostics.All(x => x.Code == "PE7005"));
    }

    #endregion

    #region Private Methods

    private static string Name(Diagnostic diagnostic) => diagnostic.Message.Split(' ')[0];
    private static string AuditJson(string direct, string transitive) => $$"""
          {
            "version": 1,
            "projects": [{
              "frameworks": [{
                "topLevelPackages": [{{direct}}],
                "transitivePackages": [{{transitive}}]
              }]
            }]
          }
          """;
    private static string FindRepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
            if (File.Exists(Path.Combine(current.FullName, "src", "Paradigm.Enterprise.slnx")))
                return current.FullName;
        throw new DirectoryNotFoundException("Repository root was not found.");
    }

    #endregion
}