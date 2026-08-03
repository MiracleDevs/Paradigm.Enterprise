using System.Diagnostics;

namespace Paradigm.Enterprise.Cli.Tests;

[TestClass]
[TestCategory("Integration")]
public class ArtifactResolutionTests
{
    #region Public Methods

    [TestMethod]
    public async Task Custom_target_path_staleness_and_malformed_output_are_not_false_clean()
    {
        var root = Path.Combine(Path.GetTempPath(), $"paradigm-output-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var project = Path.Combine(root, "Sample.csproj");
            var source = Path.Combine(root, "Program.cs");
            File.WriteAllText(project, """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <OutputType>Library</OutputType>
                    <OutputPath>artifacts\custom\</OutputPath>
                    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
                  </PropertyGroup>
                </Project>
                """);
            File.WriteAllText(source, "public sealed class SampleType { }");
            RunDotNet(root, "restore", project);
            RunDotNet(root, "build", project, "--no-restore");
            var built = AssetsReader.Read(project, null);
            Assert.IsNotNull(built.ApplicationAssembly, $"Expected evaluated output: {built.ExpectedApplicationAssembly}; actual DLLs: " + string.Join(", ", Directory.EnumerateFiles(root, "*.dll", SearchOption.AllDirectories)));
            StringAssert.Contains(built.ApplicationAssembly, Path.Combine("artifacts", "custom"));
            Assert.IsFalse(built.ApplicationAssemblyIsStale);
            File.SetLastWriteTimeUtc(source, DateTime.UtcNow.AddMinutes(1));
            var stale = AssetsReader.Read(project, null);
            Assert.IsTrue(stale.ApplicationAssemblyIsStale);
            File.SetLastWriteTimeUtc(source, DateTime.UtcNow.AddMinutes(-2));
            File.WriteAllText(built.ApplicationAssembly!, "not a managed assembly");
            File.SetLastWriteTimeUtc(built.ApplicationAssembly!, DateTime.UtcNow);
            var malformed = AssetsReader.Read(project, null);
            Assert.IsTrue(malformed.Diagnostics.Any(x => x.Code == "PE1002"));
            using var output = new StringWriter();
            var exit = await TestCliApplication.RunAsync(["validate", "--project", project, "--format", "json"], output, TextWriter.Null);
            Assert.AreEqual(3, exit);
            using var json = System.Text.Json.JsonDocument.Parse(output.ToString());
            Assert.AreEqual("error", json.RootElement.GetProperty("status").GetString());
            Assert.IsTrue(json.RootElement.GetProperty("diagnostics").EnumerateArray().Any(x => x.GetProperty("code").GetString() == "PE1002"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [TestMethod]
    public void Malformed_restore_metadata_is_reported()
    {
        var root = Path.Combine(Path.GetTempPath(), $"paradigm-assets-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "obj"));
        try
        {
            var project = Path.Combine(root, "Sample.csproj");
            File.WriteAllText(project, "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            File.WriteAllText(Path.Combine(root, "obj", "project.assets.json"), "{ broken");
            var exception = Assert.Throws<AssetsException>(() => AssetsReader.Read(project, null));
            StringAssert.Contains(exception.Message, "malformed");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [TestMethod]
    public async Task Doctor_and_validate_ignore_DACPAC_output_in_mixed_slnx()
    {
        var root = Path.Combine(Path.GetTempPath(), $"paradigm-mixed-output-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var project = Path.Combine(root, "Sample.csproj");
            File.WriteAllText(project, """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                  </PropertyGroup>
                </Project>
                """);
            File.WriteAllText(Path.Combine(root, "SampleType.cs"), "public sealed class SampleType { }");
            var databaseProject = Path.Combine(root, "Sample.Database.sqlproj");
            File.WriteAllText(databaseProject, """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <TargetPath>database\bin\Release\Sample.Database.dacpac</TargetPath>
                  </PropertyGroup>
                </Project>
                """);
            var dacpacDirectory = Path.Combine(root, "database", "bin", "Release");
            Directory.CreateDirectory(dacpacDirectory);
            File.WriteAllText(Path.Combine(dacpacDirectory, "Sample.Database.dacpac"), "not a managed assembly");
            var solution = Path.Combine(root, "Sample.slnx");
            File.WriteAllText(solution, """
                <Solution>
                  <Folder Name="/02.Modules/">
                    <Project Path="Sample.csproj" />
                    <Project Path="Sample.Database.sqlproj" />
                  </Folder>
                </Solution>
                """);
            RunDotNet(root, "restore", project);
            RunDotNet(root, "build", project, "--no-restore");
            RunDotNet(root, "restore", databaseProject);

            var databaseAssets = AssetsReader.Read(databaseProject, null);
            Assert.IsTrue(databaseAssets.Diagnostics.Any(x => x.Code == "PE1002" && x.Message.Contains("dacpac", StringComparison.OrdinalIgnoreCase)));

            using var doctorOutput = new StringWriter();
            var doctorExit = await TestCliApplication.RunAsync(["doctor", "--project", solution, "--format", "json"], doctorOutput, TextWriter.Null);
            using var validateOutput = new StringWriter();
            var validateExit = await TestCliApplication.RunAsync(["validate", "--project", solution, "--format", "json"], validateOutput, TextWriter.Null);

            Assert.AreEqual(0, doctorExit, doctorOutput.ToString());
            Assert.AreEqual(0, validateExit, validateOutput.ToString());
            StringAssert.Contains(doctorOutput.ToString(), "Sample.dll");
            Assert.IsFalse(doctorOutput.ToString().Contains("dacpac", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(validateOutput.ToString().Contains("dacpac", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(doctorOutput.ToString().Contains("PE1002", StringComparison.Ordinal));
            Assert.IsFalse(validateOutput.ToString().Contains("PE1002", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    #endregion

    #region Private Methods

    private static void RunDotNet(string workingDirectory, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);
        using var process = Process.Start(startInfo)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.AreEqual(0, process.ExitCode, stdout + Environment.NewLine + stderr);
    }

    #endregion
}
