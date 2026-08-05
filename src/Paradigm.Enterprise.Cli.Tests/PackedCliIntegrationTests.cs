using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Paradigm.Enterprise.Cli.Tests;

[TestClass]
[TestCategory("Integration")]
[DoNotParallelize]
public class PackedCliIntegrationTests
{
    #region Nested Types

    private sealed record ProcessResult(int ExitCode, string Output, string Error);

    #endregion

    #region Fields

    private static string repository = null!;
    private static string temporary = null!;
    private static string executable = null!;
    private static string providerProject = null!;
    private static string metadataProject = null!;
    private static string auditProject = null!;
    private static string auditShimDirectory = null!;
    private static string version = null!;

    #endregion

    #region Public Methods

    [ClassInitialize]
    public static void Initialize(TestContext _)
    {
        repository = FindRepositoryRoot();
        temporary = Path.Combine(Path.GetTempPath(), $"paradigm-packed-cli-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporary);
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        version = CentralVersion(repository);
        executable = RequiredFile("PARADIGM_CLI_INTEGRATION_EXECUTABLE");
        providerProject = Path.Combine(repository, "src", "Paradigm.Enterprise.Providers", "Paradigm.Enterprise.Providers.csproj");
        metadataProject = Path.Combine(repository, "src", "Paradigm.Enterprise.Tests", "Paradigm.Enterprise.Tests.csproj");
        auditProject = Path.Combine(repository, "src", "Paradigm.Enterprise.Cli.Tests", "Paradigm.Enterprise.Cli.Tests.csproj");
        var auditFixtureDirectory = Path.Combine(repository, "src", "Paradigm.Enterprise.Cli.Tests", "Fixtures", "DotNetAuditFixture", "bin", configuration, "net10.0");
        auditShimDirectory = Path.Combine(temporary, "audit-shim");
        Directory.CreateDirectory(auditShimDirectory);
        foreach (var file in Directory.EnumerateFiles(auditFixtureDirectory))
            File.Copy(file, Path.Combine(auditShimDirectory, Path.GetFileName(file)));
        var fixtureExecutable = Path.Combine(auditShimDirectory, OperatingSystem.IsWindows() ? "DotNetAuditFixture.exe" : "DotNetAuditFixture");
        var dotnetShim = Path.Combine(auditShimDirectory, OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
        File.Move(fixtureExecutable, dotnetShim);
    }

    [ClassCleanup]
    public static void Cleanup()
    {
        if (Directory.Exists(temporary))
            Directory.Delete(temporary, recursive: true);
    }

    [TestMethod]
    [Timeout(180_000)]
    public async Task Packed_cli_core_routes_use_the_real_program_composition()
    {
        var help = await Run();
        Assert.AreEqual(0, help.ExitCode, help.Error);
        StringAssert.StartsWith(help.Output, "Paradigm.Enterprise CLI");
        var release = await Run("--version");
        Assert.AreEqual(0, release.ExitCode, release.Error);
        Assert.AreEqual(version, release.Output.Trim());
        var doctor = await Run("doctor", "--project", providerProject, "--format", "json");
        AssertJson(doctor, "doctor");
    }

    [TestMethod]
    [Timeout(180_000)]
    public async Task Packed_cli_metadata_routes_preserve_json_schema_and_guidance()
    {
        var search = await Run("api", "search", "EditProvider", "--project", metadataProject, "--limit", "5", "--format", "json");
        using var searchJson = AssertJson(search, "api search");
        Assert.IsTrue(searchJson.RootElement.GetProperty("results").GetArrayLength() > 0);
        var show = await Run("api", "show", "IEditProvider", "--project", metadataProject, "--package", "Providers", "--format", "json");
        using var showJson = AssertJson(show, "api show");
        StringAssert.Contains(showJson.RootElement.GetProperty("results")[0].GetProperty("name").GetString()!, "IEditProvider");
        var guide = await Run("api", "guide", "IEditProvider", "--project", metadataProject, "--package", "Providers", "--format", "json");
        using var guideJson = AssertJson(guide, "api guide");
        StringAssert.EndsWith(guideJson.RootElement.GetProperty("guide").GetProperty("symbol").GetString()!, ".IEditProvider<TView, TId>");
        using var inspect = AssertJson(await Run("inspect", "--project", providerProject, "--format", "json"), "inspect");
        using var validate = AssertJson(await Run("validate", "--project", providerProject, "--format", "json"), "validate");
    }

    [TestMethod]
    [Timeout(180_000)]
    public async Task Packed_cli_check_and_package_routes_use_the_single_installed_tool()
    {
        using var list = AssertJson(await Run("checks", "list", "--format", "json"), "checks list");
        Assert.AreEqual("csharp", list.RootElement.GetProperty("checks").GetProperty("builtIn")[0].GetProperty("id").GetString());
        var violationsProject = Path.Combine(repository, "src", "Paradigm.Enterprise.Cli.Tests", "Fixtures", "SemanticViolations", "SemanticViolations.csproj");
        var checkResult = await Run("checks", "run", "--project", violationsProject, "--format", "json");
        Assert.AreEqual(1, checkResult.ExitCode, checkResult.Error);
        using var checks = JsonDocument.Parse(checkResult.Output);
        Assert.AreEqual("checks run", checks.RootElement.GetProperty("command").GetString());
        Assert.AreEqual("csharp", checks.RootElement.GetProperty("checks").GetProperty("executed")[0].GetString());
        var generationAssembly = Path.Combine(repository, "src", "Paradigm.Enterprise.Cli.Tests", "Fixtures", "GoodPractices", "bin", new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name, "net10.0", "GoodPractices.dll");
        var generationOutput = Path.Combine(temporary, "generated");
        using var generation = AssertJson(await Run("generate", "json", "--project-name", "GoodPractices", "--assembly", generationAssembly, "--output", generationOutput, "--format", "json"), "generate json");
        Assert.IsTrue(Directory.Exists(Path.Combine(generationOutput, "JsonSerializerContexts")));
        using var packageCheck = AssertJson(await Run("packages", "check", "--project", providerProject, "--format", "json"), "packages check");
        Assert.IsTrue(packageCheck.RootElement.GetProperty("results").GetArrayLength() > 0);
        using var packageAudit = AssertJson(await Run(new Dictionary<string, string?> { ["PATH"] = auditShimDirectory + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH") }, "packages", "audit", "--project", auditProject, "--format", "json"), "packages audit");
        Assert.AreEqual(0, packageAudit.RootElement.GetProperty("diagnostics").GetArrayLength());
    }

    [TestMethod]
    [Timeout(180_000)]
    public async Task Packed_cli_scaffolds_with_the_real_program_composition()
    {
        var template = Path.Combine(temporary, "scaffold-template", "src");
        var templateProject = Path.Combine(template, "Paradigm.Web.ApiTemplate.WebApi");
        Directory.CreateDirectory(templateProject);
        File.WriteAllText(Path.Combine(template, "Paradigm.Web.ApiTemplate.sln"), "Microsoft Visual Studio Solution File");
        File.WriteAllText(Path.Combine(templateProject, "Paradigm.Web.ApiTemplate.WebApi.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"Paradigm.Enterprise.WebApi\" Version=\"1.0.0\" /></ItemGroup></Project>");
        var output = Path.Combine(temporary, "scaffold-output");
        using var result = AssertJson(await Run(
            "scaffold",
            "solution",
            "--template-root",
            Path.GetDirectoryName(template)!,
            "--name",
            "Contoso.Packed",
            "--output",
            output,
            "--paradigm-version",
            version,
            "--format",
            "json"), "scaffold solution");
        Assert.IsTrue(File.Exists(Path.Combine(output, "start.sh")));
        Assert.IsTrue(File.Exists(Path.Combine(output, ".github", "problem-matchers", "paradigm.json")));
        Assert.IsTrue(File.Exists(Path.Combine(output, ".github", "workflows", "quality.yml")));
        StringAssert.Contains(File.ReadAllText(Path.Combine(output, "src", "Contoso.Packed.WebApi", "Contoso.Packed.WebApi.csproj")), $"Version=\"{version}\"");
    }

    #endregion

    #region Private Methods

    private static JsonDocument AssertJson(ProcessResult result, string command)
    {
        Assert.AreEqual(0, result.ExitCode, result.Error + Environment.NewLine + result.Output);
        var document = JsonDocument.Parse(result.Output);
        Assert.AreEqual("1.1", document.RootElement.GetProperty("schemaVersion").GetString());
        Assert.AreEqual(command, document.RootElement.GetProperty("command").GetString());
        Assert.AreEqual("success", document.RootElement.GetProperty("status").GetString());
        return document;
    }

    private static Task<ProcessResult> Run(params string[] arguments) => Run(null, arguments);
    private static Task<ProcessResult> Run(IReadOnlyDictionary<string, string?>? environment, params string[] arguments) => RunProcess(executable, arguments, repository, environment);
    private static async Task<ProcessResult> RunProcess(string processName, IReadOnlyList<string> arguments, string workingDirectory, IReadOnlyDictionary<string, string?>? environment = null)
    {
        using var process = new Process
        {
            StartInfo = new()
            {
                FileName = processName,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = new UTF8Encoding(false),
                StandardErrorEncoding = new UTF8Encoding(false),
                CreateNoWindow = true
            }
        };
        foreach (var argument in arguments)
            process.StartInfo.ArgumentList.Add(argument);
        if (environment is not null)
            foreach (var (name, value) in environment)
                process.StartInfo.Environment[name] = value;
        process.Start();
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
            throw;
        }

        return new(process.ExitCode, await output, await error);
    }

    private static string CentralVersion(string root)
    {
        var text = File.ReadAllText(Path.Combine(root, "build", "Paradigm.Version.props"));
        const string start = "<ParadigmEnterpriseVersion>";
        var index = text.IndexOf(start, StringComparison.Ordinal) + start.Length;
        return text[index..text.IndexOf("</ParadigmEnterpriseVersion>", index, StringComparison.Ordinal)];
    }

    private static string RequiredFile(string variable)
    {
        var path = Environment.GetEnvironmentVariable(variable);
        Assert.IsFalse(string.IsNullOrWhiteSpace(path), $"Set {variable} to the absolute path prepared by build/quality.sh or the PR quality workflow before running Integration tests.");
        Assert.IsTrue(Path.IsPathFullyQualified(path), $"{variable} must contain an absolute path, but was '{path}'.");
        Assert.IsTrue(File.Exists(path), $"{variable} points to missing file '{path}'. Run build/quality.sh to prepare and execute the complete suite.");
        return path;
    }

    private static string FindRepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
            if (File.Exists(Path.Combine(current.FullName, "src", "Paradigm.Enterprise.slnx")))
                return current.FullName;
        throw new DirectoryNotFoundException("Repository root was not found.");
    }

    #endregion
}