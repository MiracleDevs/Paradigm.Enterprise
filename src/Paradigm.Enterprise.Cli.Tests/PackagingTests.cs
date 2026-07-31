using System.Diagnostics;

namespace Paradigm.Enterprise.Cli.Tests;

[TestClass]
[DoNotParallelize]
public class PackagingTests
{
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
                        "pack", Path.Combine(repository, "src", "Paradigm.Enterprise.slnx"),
                        "--configuration", configuration, "--no-build", "--output", output,
                        $"-p:PackageVersion={version}"
                    }
                }
            };
            process.Start();
            var standardOutput = process.StandardOutput.ReadToEndAsync();
            var standardError = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            Assert.AreEqual(0, process.ExitCode,
                (await standardError) + Environment.NewLine + (await standardOutput));

            Assert.IsTrue(File.Exists(Path.Combine(
                output, $"Paradigm.Enterprise.Cli.{version}.nupkg")));
            Assert.IsFalse(Directory.EnumerateFiles(output, "CheckPackFixture.*.nupkg").Any());
            Assert.IsFalse(Directory.EnumerateFiles(output, "SemanticViolations.*.nupkg").Any());
        }
        finally
        {
            Directory.Delete(output, recursive: true);
        }
    }

    private static string FindRepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory);
             current is not null;
             current = current.Parent)
            if (File.Exists(Path.Combine(current.FullName, "src", "Paradigm.Enterprise.slnx")))
                return current.FullName;
        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
