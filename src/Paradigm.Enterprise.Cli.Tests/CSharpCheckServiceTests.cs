using Paradigm.Enterprise.Checks.CSharp;

namespace Paradigm.Enterprise.Cli.Tests;
[TestClass]
public class CSharpCheckServiceTests
{
#region Public Methods
    [TestMethod]
    public void Source_layout_rules_report_invalid_files_and_allow_valid_and_generated_files()
    {
        var diagnostics = AnalyzeGoodPracticesFixture();
        Assert.HasCount(1, diagnostics.Where(diagnostic => diagnostic.Code == "PE3105"));
        StringAssert.Contains(diagnostics.Single(diagnostic => diagnostic.Code == "PE3105").Location!, "MultipleTypes.cs");
        Assert.IsFalse(diagnostics.Any(diagnostic => diagnostic.Location?.Contains("Generated.g.cs", StringComparison.OrdinalIgnoreCase) == true));
        Assert.IsFalse(diagnostics.Any(diagnostic => diagnostic.Location?.Contains("GoodLayout.cs", StringComparison.OrdinalIgnoreCase) == true));
    }

    [TestMethod]
    public void Member_layout_rule_reports_order_and_exact_region_names()
    {
        var diagnostics = AnalyzeGoodPracticesFixture().Where(diagnostic => diagnostic.Code == "PE3106").ToArray();
        Assert.IsNotEmpty(diagnostics);
        Assert.IsTrue(diagnostics.All(diagnostic => diagnostic.Location?.Contains("BadLayout.cs", StringComparison.OrdinalIgnoreCase) == true));
        Assert.IsTrue(diagnostics.Any(diagnostic => diagnostic.Message.Contains("#region Fields", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void Existing_semantic_rules_remain_built_in()
    {
        var project = FixtureProject("SemanticViolations", "SemanticViolations.csproj");
        var diagnostics = CSharpCheckService.Analyze(new(project, "net10.0"));
        Assert.IsTrue(diagnostics.Any(diagnostic => diagnostic.Code == "PE3103"));
        Assert.IsTrue(diagnostics.Any(diagnostic => diagnostic.Code == "PE3104"));
    }

#endregion
#region Private Methods
    private static IReadOnlyList<CSharpCheckDiagnostic> AnalyzeGoodPracticesFixture() => CSharpCheckService.Analyze(new(FixtureProject("GoodPractices", "GoodPractices.csproj"), "net10.0"));
    private static string FixtureProject(string directory, string project)
    {
        var repository = FindRepositoryRoot();
        return Path.Combine(repository, "src", "Paradigm.Enterprise.Cli.Tests", "Fixtures", directory, project);
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
