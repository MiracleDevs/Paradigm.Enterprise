using System.Diagnostics;
using System.IO.Compression;

namespace Paradigm.Enterprise.Cli.Tests;
[TestClass]
[DoNotParallelize]
public class PackagingTests
{
#region Public Methods
    [TestMethod]
    [Timeout(120_000)]
    public async Task Solution_pack_never_emits_test_fixture_packages()
    {
        var repository = FindRepositoryRoot();
        var output = Path.Combine(Path.GetTempPath(), $"paradigm-pack-output-{Guid.NewGuid():N}");
        Directory.CreateDirectory(output);
        try
        {
            var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
            var version = $"0.0.0-pack-verification-{Guid.NewGuid():N}";
            using var process = new Process
            {
                StartInfo = new()
                {
                    FileName = "dotnet",
                    WorkingDirectory = repository,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    ArgumentList =
                    {
                        "pack",
                        Path.Combine(repository, "src", "Paradigm.Enterprise.slnx"),
                        "--configuration",
                        configuration,
                        "--no-build",
                        "--output",
                        output,
                        $"-p:PackageVersion={version}"}
                }
            };
            process.Start();
            var standardOutput = process.StandardOutput.ReadToEndAsync();
            var standardError = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            Assert.AreEqual(0, process.ExitCode, (await standardError) + Environment.NewLine + (await standardOutput));
            var package = Path.Combine(output, $"Paradigm.Enterprise.Cli.{version}.nupkg");
            Assert.IsTrue(File.Exists(package));
            using var archive = ZipFile.OpenRead(package);
            var entries = archive.Entries.Select(entry => entry.FullName).ToArray();
            Assert.IsTrue(entries.Any(entry => entry.EndsWith("/Paradigm.Enterprise.Checks.CSharp.dll", StringComparison.Ordinal)));
            Assert.IsTrue(entries.Any(entry => entry.EndsWith("/Paradigm.Enterprise.CodeGenerator.dll", StringComparison.Ordinal)));
            Assert.IsTrue(entries.Any(entry => entry.EndsWith("/paradigm-code-generator.settings.json", StringComparison.Ordinal)));
            Assert.IsFalse(Directory.EnumerateFiles(output, "Paradigm.Enterprise.Checks.CSharp.*.nupkg").Any());
            Assert.IsFalse(Directory.EnumerateFiles(output, "Paradigm.Enterprise.CodeGenerator.*.nupkg").Any());
            Assert.IsFalse(Directory.EnumerateFiles(output, "SemanticViolations.*.nupkg").Any());
        }
        finally
        {
            Directory.Delete(output, recursive: true);
        }
    }

#endregion
#region Private Methods
    private static string FindRepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
            if (File.Exists(Path.Combine(current.FullName, "src", "Paradigm.Enterprise.slnx")))
                return current.FullName;
        throw new DirectoryNotFoundException("Repository root was not found.");
    }
#endregion
}
