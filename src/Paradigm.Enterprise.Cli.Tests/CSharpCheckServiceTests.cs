using Paradigm.Enterprise.Checks.CSharp;

namespace Paradigm.Enterprise.Cli.Tests;

[TestClass]
[TestCategory("Integration")]
public class CSharpCheckServiceTests
{
    #region Fields

    private static readonly Lazy<IReadOnlyList<CSharpCheckDiagnostic>> goodPracticesDiagnostics = new(() => CSharpCheckService.Analyze(new(FixtureProject("GoodPractices", "GoodPractices.csproj"), "net10.0")));
    private static readonly Lazy<IReadOnlyList<CSharpCheckDiagnostic>> semanticDiagnostics = new(() => CSharpCheckService.Analyze(new(FixtureProject("SemanticViolations", "SemanticViolations.csproj"), "net10.0")));

    #endregion

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
        Assert.IsTrue(diagnostics.Any(diagnostic => diagnostic.Message.Contains("#region Fields", StringComparison.Ordinal)));
        Assert.IsTrue(diagnostics.Any(diagnostic =>
            diagnostic.Location?.Contains("BadSpacing.cs", StringComparison.OrdinalIgnoreCase) == true &&
            diagnostic.Message.Contains("exactly one empty line", StringComparison.Ordinal)));
        Assert.IsTrue(diagnostics.All(diagnostic =>
            diagnostic.Location?.Contains("BadLayout.cs", StringComparison.OrdinalIgnoreCase) == true ||
            diagnostic.Location?.Contains("BadSpacing.cs", StringComparison.OrdinalIgnoreCase) == true));
    }

    [TestMethod]
    public void Existing_semantic_rules_remain_built_in()
    {
        var diagnostics = semanticDiagnostics.Value;
        Assert.IsTrue(diagnostics.Any(diagnostic => diagnostic.Code == "PE3103"));
        Assert.IsTrue(diagnostics.Any(diagnostic => diagnostic.Code == "PE3104"));
    }

    [TestMethod]
    public void MySql_routine_boundary_is_recognized_without_exempting_mixed_ef_pagination()
    {
        var diagnostics = semanticDiagnostics.Value
            .Where(diagnostic => diagnostic.Code == "PE3103" &&
                                 diagnostic.Message.Contains("MySqlOrderRepository.", StringComparison.Ordinal))
            .ToArray();

        Assert.HasCount(3, diagnostics);
        foreach (var method in new[] { "SearchMixedTypedProcedureAndEf", "SearchMixedCommandProcedureAndEf" })
        {
            var diagnostic = diagnostics.Single(item => item.Message.Contains($".{method} ", StringComparison.Ordinal));
            StringAssert.Contains(diagnostic.Message, "Calling a stored procedure elsewhere");
        }

        var lookalike = diagnostics.Single(item => item.Message.Contains(".SearchLookalikeProcedureAndEf ", StringComparison.Ordinal));
        StringAssert.Contains(lookalike.Message, "Use a stored-procedure repository boundary");
        Assert.IsFalse(diagnostics.Any(item => item.Message.Contains(".SearchTypedProcedure ", StringComparison.Ordinal)));
    }

    #endregion

    #region Private Methods

    private static IReadOnlyList<CSharpCheckDiagnostic> AnalyzeGoodPracticesFixture() => goodPracticesDiagnostics.Value;
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