using System.Text.Json;
using System.Text.RegularExpressions;

namespace Paradigm.Enterprise.Cli.Tests;

[TestClass]
public class SolutionScaffolderTests
{
    #region Public Methods

    [TestMethod]
    public void Dry_run_does_not_write()
    {
        var temporary = CreateTemporary();
        try
        {
            var template = CreateTemplate(temporary);
            var output = Path.Combine(temporary, "output");
            var result = new SolutionScaffolder().Scaffold(Options(template, output, dryRun: true), CancellationToken.None);
            Assert.IsTrue(result.Files.Count > 0);
            CollectionAssert.Contains(result.Files.ToArray(), Path.Combine(output, ".github", "problem-matchers", "paradigm.json"));
            CollectionAssert.Contains(result.Files.ToArray(), Path.Combine(output, ".github", "workflows", "quality.yml"));
            Assert.IsFalse(Directory.Exists(output));
        }
        finally
        {
            Directory.Delete(temporary, recursive: true);
        }
    }

    [TestMethod]
    public void Scaffold_replaces_tokens_versions_and_guids_without_changing_source()
    {
        var temporary = CreateTemporary();
        try
        {
            var template = CreateTemplate(temporary);
            var sourceSnapshot = Directory.EnumerateFiles(template, "*", SearchOption.AllDirectories).ToDictionary(path => Path.GetRelativePath(template, path), File.ReadAllBytes);
            var output = Path.Combine(temporary, "output");
            new SolutionScaffolder().Scaffold(Options(template, output), CancellationToken.None);

            var project = Path.Combine(output, "src", "Contoso.Product.WebApi", "Contoso.Product.WebApi.csproj");
            Assert.IsTrue(File.Exists(project));
            StringAssert.Contains(File.ReadAllText(project), "Version=\"1.1.0\"");
            StringAssert.Contains(File.ReadAllText(Path.Combine(Path.GetDirectoryName(project)!, "Program.cs")), "namespace Contoso.Product.WebApi;");
            CollectionAssert.AreEqual(new byte[] { 0, 1, 255, 8 }, File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(project)!, "icon.ico")));
            var solution = File.ReadAllText(Path.Combine(output, "src", "Contoso.Product.sln"));
            Assert.IsFalse(solution.Contains("{11111111-2222-3333-4444-555555555555}", StringComparison.Ordinal));
            Assert.AreEqual(1, Regex.Matches(solution, "\\{[0-9A-F]{8}(?:-[0-9A-F]{4}){3}-[0-9A-F]{12}\\}").Select(match => match.Value).Distinct().Count());
            foreach (var (relative, bytes) in sourceSnapshot)
                CollectionAssert.AreEqual(bytes, File.ReadAllBytes(Path.Combine(template, relative)));

            var starter = File.ReadAllText(Path.Combine(output, "start.sh"));
            StringAssert.Contains(starter, "ensure_local_tool \"Aspire.Cli\" \"aspire\"");
            StringAssert.Contains(starter, "dotnet tool install \"${package}\"");
            StringAssert.Contains(starter, "Paradigm.Enterprise.Cli");
            StringAssert.Contains(starter, "docker info");
            StringAssert.Contains(starter, "start|stop|doctor|help");
            StringAssert.Contains(starter, "dotnet --list-sdks");
            StringAssert.Contains(starter, "src/Contoso.Product.AppHost/Contoso.Product.AppHost.csproj");
            StringAssert.Contains(starter, "aspire stop --apphost");
            Assert.IsFalse(starter.Contains("sqlcmd", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(starter.Contains("sqlpackage", StringComparison.OrdinalIgnoreCase));

            var problemMatcher = Path.Combine(output, ".github", "problem-matchers", "paradigm.json");
            using var matcherDocument = JsonDocument.Parse(File.ReadAllText(problemMatcher));
            var matcherPattern = matcherDocument.RootElement.GetProperty("problemMatcher")[0].GetProperty("pattern")[0];
            Assert.AreEqual(1, matcherPattern.GetProperty("code").GetInt32());
            Assert.AreEqual(2, matcherPattern.GetProperty("severity").GetInt32());
            Assert.AreEqual(3, matcherPattern.GetProperty("message").GetInt32());
            Assert.AreEqual(4, matcherPattern.GetProperty("file").GetInt32());
            var matcherRegex = new Regex(matcherPattern.GetProperty("regexp").GetString()!, RegexOptions.CultureInvariant);
            var warning = matcherRegex.Match("PE7008 warning: Sample.Package is outdated. [/repo/src/Contoso.Product.WebApi/Contoso.Product.WebApi.csproj]");
            Assert.IsTrue(warning.Success);
            Assert.AreEqual("PE7008", warning.Groups[1].Value);
            Assert.AreEqual("warning", warning.Groups[2].Value);
            Assert.AreEqual("Sample.Package is outdated.", warning.Groups[3].Value);
            Assert.AreEqual("/repo/src/Contoso.Product.WebApi/Contoso.Product.WebApi.csproj", warning.Groups[4].Value);
            var error = matcherRegex.Match("PE1002 error: Project resolution failed.");
            Assert.IsTrue(error.Success);
            Assert.AreEqual("error", error.Groups[2].Value);

            var workflow = File.ReadAllText(Path.Combine(output, ".github", "workflows", "quality.yml"));
            StringAssert.Contains(workflow, "::add-matcher::${{ github.workspace }}/.github/problem-matchers/paradigm.json");
            StringAssert.Contains(workflow, "Paradigm.Enterprise.Cli --version 1.1.0");
            StringAssert.Contains(workflow, "dotnet restore src/Contoso.Product.sln");
            StringAssert.Contains(workflow, "paradigm packages audit --project src/Contoso.Product.sln");
            StringAssert.Contains(workflow, "paradigm validate --project src/Contoso.Product.sln");
            StringAssert.Contains(workflow, "paradigm checks run --project src/Contoso.Product.sln");
            Assert.IsFalse(workflow.Contains("__PARADIGM_VERSION__", StringComparison.Ordinal));
            Assert.IsFalse(workflow.Contains("__SOLUTION_PATH__", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(temporary, recursive: true);
        }
    }

    [TestMethod]
    public void Scaffold_refuses_nonempty_output()
    {
        var temporary = CreateTemporary();
        try
        {
            var template = CreateTemplate(temporary);
            var output = Path.Combine(temporary, "output");
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "owned.txt"), "user data");
            var exception = Assert.Throws<ArgumentException>(() => new SolutionScaffolder().Scaffold(Options(template, output), CancellationToken.None));
            StringAssert.Contains(exception.Message, "must be empty");
        }
        finally
        {
            Directory.Delete(temporary, recursive: true);
        }
    }

    [TestMethod]
    public void Scaffold_preserves_slnx_format()
    {
        var temporary = CreateTemporary();
        try
        {
            var template = CreateTemplate(temporary);
            var source = Path.Combine(template, "src", "Paradigm.Web.ApiTemplate.sln");
            File.Move(source, Path.ChangeExtension(source, ".slnx"));
            var output = Path.Combine(temporary, "output");
            new SolutionScaffolder().Scaffold(Options(template, output), CancellationToken.None);
            Assert.IsTrue(File.Exists(Path.Combine(output, "src", "Contoso.Product.slnx")));
            Assert.IsFalse(File.Exists(Path.Combine(output, "src", "Contoso.Product.sln")));
            StringAssert.Contains(File.ReadAllText(Path.Combine(output, ".github", "workflows", "quality.yml")), "--project src/Contoso.Product.slnx");
        }
        finally
        {
            Directory.Delete(temporary, recursive: true);
        }
    }

    #endregion

    #region Private Methods

    private static string CreateTemporary()
    {
        var path = Path.Combine(Path.GetTempPath(), $"paradigm-scaffolder-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static string CreateTemplate(string root)
    {
        var template = Path.Combine(root, "template");
        var source = Path.Combine(template, "src");
        var project = Path.Combine(source, "Paradigm.Web.ApiTemplate.WebApi");
        Directory.CreateDirectory(project);
        const string guid = "{11111111-2222-3333-4444-555555555555}";
        File.WriteAllText(Path.Combine(source, "Paradigm.Web.ApiTemplate.sln"), $"Project(\"{guid}\") = \"Paradigm.Web.ApiTemplate.WebApi\", \"Paradigm.Web.ApiTemplate.WebApi\\Paradigm.Web.ApiTemplate.WebApi.csproj\", \"{guid}\"");
        File.WriteAllText(Path.Combine(project, "Paradigm.Web.ApiTemplate.WebApi.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"Paradigm.Enterprise.WebApi\" Version=\"1.0.23\" /></ItemGroup></Project>");
        File.WriteAllText(Path.Combine(project, "Program.cs"), "namespace $ext_safeprojectname$.WebApi;");
        File.WriteAllBytes(Path.Combine(project, "icon.ico"), [0, 1, 255, 8]);
        return template;
    }

    private static ScaffoldSolutionOptions Options(string template, string output, bool dryRun = false) => new(template, "Contoso.Product", output, "1.1.0", dryRun, OutputFormat.Text);

    #endregion
}
