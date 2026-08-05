using System.IO.Compression;

namespace Paradigm.Enterprise.Cli.Tests;

[TestClass]
[TestCategory("Integration")]
[DoNotParallelize]
public class PackagingTests
{
    #region Public Methods

    [TestMethod]
    public void Prepared_solution_package_never_emits_test_fixture_packages()
    {
        var package = RequiredFile("PARADIGM_CLI_INTEGRATION_PACKAGE");
        var output = Path.GetDirectoryName(package)!;
        using var archive = ZipFile.OpenRead(package);
        var entries = archive.Entries.Select(entry => entry.FullName).ToArray();
        Assert.IsTrue(entries.Any(entry => entry.EndsWith("/Paradigm.Enterprise.Checks.CSharp.dll", StringComparison.Ordinal)));
        Assert.IsTrue(entries.Any(entry => entry.EndsWith("/Paradigm.Enterprise.CodeGenerator.dll", StringComparison.Ordinal)));
        Assert.IsTrue(entries.Any(entry => entry.EndsWith("/paradigm-code-generator.settings.json", StringComparison.Ordinal)));
        Assert.IsFalse(Directory.EnumerateFiles(output, "Paradigm.Enterprise.Checks.CSharp.*.nupkg").Any());
        Assert.IsFalse(Directory.EnumerateFiles(output, "Paradigm.Enterprise.CodeGenerator.*.nupkg").Any());
        Assert.IsFalse(Directory.EnumerateFiles(output, "SemanticViolations.*.nupkg").Any());
    }

    #endregion

    #region Private Methods

    private static string RequiredFile(string variable)
    {
        var path = Environment.GetEnvironmentVariable(variable);
        Assert.IsFalse(string.IsNullOrWhiteSpace(path), $"Set {variable} to the absolute package path prepared by build/quality.sh or the PR quality workflow before running Integration tests.");
        Assert.IsTrue(Path.IsPathFullyQualified(path), $"{variable} must contain an absolute path, but was '{path}'.");
        Assert.IsTrue(File.Exists(path), $"{variable} points to missing file '{path}'. Run build/quality.sh to prepare and execute the complete suite.");
        return path;
    }

    #endregion
}