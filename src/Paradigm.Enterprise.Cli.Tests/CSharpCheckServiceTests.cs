using Paradigm.Enterprise.Checks.CSharp;

namespace Paradigm.Enterprise.Cli.Tests;

[TestClass]
[TestCategory("Integration")]
public class CSharpCheckServiceTests
{
    #region Nested Types

    private sealed record RepositorySqlDiagnostic(
        string Code,
        string Severity,
        string Member,
        string Construct,
        string File,
        int Line,
        int Column);

    private sealed record CliChecksResult(
        int ExitCode,
        System.Text.Json.JsonElement[] Diagnostics,
        string[] ExecutedChecks);

    #endregion

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
        var diagnostics = AnalyzeSemanticFixture();
        Assert.IsTrue(diagnostics.Any(diagnostic => diagnostic.Code == "PE3103"));
        Assert.IsTrue(diagnostics.Any(diagnostic => diagnostic.Code == "PE3104"));
    }

    [TestMethod]
    public void Repository_SQL_policy_reports_semantic_sinks_and_declarations_without_false_positives()
    {
        RepositorySqlDiagnostic[] expected =
        [
            ExpectedRepositorySql("SemanticViolations.GeneratedRawSqlRepository.Statement", "SQL-bearing declaration", "GeneratedRawSqlRepository.g.cs", 8, 25),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.PartialSql", "SQL-bearing declaration", "RawSqlRepository.Partial.cs", 7, 5),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.RuntimeSqlAsync", "SemanticViolations.EquivalentSqlExtensions.ExecuteStatementAsync", "RawSqlRepository.cs", 100, 15),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.RuntimeSqlAsync", "SemanticViolations.EquivalentSqlExtensions.QueryContractInterpolatedAsync", "RawSqlRepository.cs", 101, 35),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.DapperLikeAsync", "Dapper.DapperLikeExtensions.QueryAsync", "RawSqlRepository.cs", 107, 15),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.DapperLikeAsync", "Dapper.DapperLikeExtensions.ExecuteAsync", "RawSqlRepository.cs", 108, 15),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.DapperLikeAsync", "Dapper.DapperLikeExtensions.ExecuteReaderAsync", "RawSqlRepository.cs", 109, 15),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.DapperLikeAsync", "Dapper.DapperLikeExtensions.ExecuteScalarAsync", "RawSqlRepository.cs", 110, 15),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.DapperLikeAsync", "SemanticViolations.EquivalentSqlExtensions.QueryRowsAsync", "RawSqlRepository.cs", 111, 15),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.DapperLikeAsync", "SemanticViolations.EquivalentSqlExtensions.QueryContractAsync", "RawSqlRepository.cs", 112, 15),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.DapperLikeAsync", "Dapper.DapperLikeExtensions.ExecuteAsync", "RawSqlRepository.cs", 113, 15),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.HelperSql", "SQL-bearing declaration", "RawSqlRepository.cs", 116, 5),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.LocalHelperSql", "SQL-bearing declaration", "RawSqlRepository.cs", 120, 22),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.Query", "SQL-bearing declaration", "RawSqlRepository.cs", 121, 9),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.BuildSqlLiteral", "SQL-bearing declaration", "RawSqlRepository.cs", 129, 5),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.SelectSql", "SQL-bearing declaration", "RawSqlRepository.cs", 17, 26),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.DeleteSql", "SQL-bearing declaration", "RawSqlRepository.cs", 34, 5),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.RawSqlRepository", "SQL-bearing assignment", "RawSqlRepository.cs", 46, 9),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.RawSqlRepository", "SQL-bearing assignment", "RawSqlRepository.cs", 47, 9),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.RawSqlRepository", "SQL-bearing assignment", "RawSqlRepository.cs", 48, 9),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.RawSqlRepository", "SQL-bearing assignment", "RawSqlRepository.cs", 49, 9),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.EfRawSqlAsync", "Microsoft.EntityFrameworkCore.RelationalQueryableExtensions.FromSqlRaw", "RawSqlRepository.cs", 64, 15),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.EfRawSqlAsync", "Microsoft.EntityFrameworkCore.RelationalQueryableExtensions.FromSqlInterpolated", "RawSqlRepository.cs", 65, 15),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.EfRawSqlAsync", "Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRawAsync", "RawSqlRepository.cs", 66, 15),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.EfRawSqlAsync", "Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlInterpolatedAsync", "RawSqlRepository.cs", 67, 15),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.EfRawSqlAsync", "Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.SqlQueryRaw", "RawSqlRepository.cs", 68, 15),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.EfRawSqlAsync", "Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.SqlQuery", "RawSqlRepository.cs", 69, 15),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.AdoAsync", "System.Data.Common.DbConnection.CreateCommand", "RawSqlRepository.cs", 74, 35),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.AdoAsync", "System.Data.Common.DbCommand.CommandText", "RawSqlRepository.cs", 75, 9),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.AdoAsync", "System.Data.Common.DbCommand.ExecuteReaderAsync", "RawSqlRepository.cs", 76, 15),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.AdoAsync", "Microsoft.Data.SqlClient.SqlCommand.ctor", "RawSqlRepository.cs", 77, 39),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.AdoAsync", "Microsoft.Data.SqlClient.SqlCommand.CommandText", "RawSqlRepository.cs", 79, 13),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.AdoAsync", "System.Data.Common.DbCommand.ExecuteScalarAsync", "RawSqlRepository.cs", 81, 15),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.AdoAsync", "System.Data.IDbConnection.CreateCommand", "RawSqlRepository.cs", 82, 37),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.AdoAsync", "System.Data.IDbCommand.CommandText", "RawSqlRepository.cs", 83, 9),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.AdoAsync", "System.Data.IDbCommand.ExecuteReader", "RawSqlRepository.cs", 84, 9),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.ConditionalAdoAsync", "System.Data.Common.DbConnection.CreateCommand", "RawSqlRepository.cs", 89, 24),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.ConditionalAdoAsync", "System.Data.Common.DbCommand.ExecuteReader", "RawSqlRepository.cs", 90, 29),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.ConditionalAdoAsync", "Dapper.DapperLikeExtensions.QueryAsync", "RawSqlRepository.cs", 91, 27),
            ExpectedRepositorySql("SemanticViolations.RawSqlRepository.ConditionalAdoAsync", "SemanticViolations.EquivalentSqlExtensions.ExecuteStatementAsync", "RawSqlRepository.cs", 93, 27),
            ExpectedRepositorySql("SemanticViolations.SourceGeneratedRawSqlRepository.Statement", "SQL-bearing declaration", "SourceGeneratedRawSqlRepository.g.cs", 8, 25)
        ];
        var actual = AnalyzeSemanticFixture()
            .Where(diagnostic => diagnostic.Code == "PE3107")
            .Select(ParseRepositorySqlDiagnostic)
            .ToArray();

        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Repository_SQL_policy_is_deterministic_and_has_unique_code_location_messages()
    {
        var first = AnalyzeSemanticFixture().Where(diagnostic => diagnostic.Code == "PE3107").ToArray();
        var second = CSharpCheckService.Analyze(new(
                FixtureProject("SemanticViolations", "SemanticViolations.csproj"), "net10.0"))
            .Where(diagnostic => diagnostic.Code == "PE3107")
            .ToArray();
        CollectionAssert.AreEqual(first, second);
        Assert.HasCount(first.Length, first.DistinctBy(diagnostic =>
            (diagnostic.Code, diagnostic.Location, diagnostic.Message)));
        CollectionAssert.AreEqual(first,
            first.OrderBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.Location, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
                .ToArray());
    }

    [TestMethod]
    public void Repository_SQL_policy_skips_projects_explicitly_marked_as_tests()
    {
        var diagnostics = CSharpCheckService.Analyze(new(
            FixtureProject("RepositorySqlTestDouble", "RepositorySqlTestDouble.csproj"), "net10.0"));
        Assert.IsFalse(diagnostics.Any(diagnostic => diagnostic.Code == "PE3107"));
    }

    [TestMethod]
    public void Incomplete_semantic_compilation_fails_closed_before_repository_SQL_policy_runs()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => CSharpCheckService.Analyze(new(
            FixtureProject("SemanticIncomplete", "SemanticIncomplete.csproj"), "net10.0")));
        StringAssert.Contains(exception.Message, "Semantic compilation");
        StringAssert.Contains(exception.Message, "MissingReference");
    }

    [TestMethod]
    public async Task Repository_SQL_policy_supports_exact_unexpired_suppression_and_restores_expired_diagnostic()
    {
        var project = FixtureProject("SemanticViolations", "SemanticViolations.csproj");
        var target = AnalyzeSemanticFixture().Single(diagnostic =>
            diagnostic.Code == "PE3107" &&
            diagnostic.Message.Contains("RelationalQueryableExtensions.FromSqlRaw", StringComparison.Ordinal));
        var location = Path.GetFileName(target.Location!);
        var directory = Path.Combine(Path.GetTempPath(), $"paradigm-pe3107-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var active = Path.Combine(directory, "active.json");
            var expired = Path.Combine(directory, "expired.json");
            File.WriteAllText(active, SuppressionConfiguration(
                "PE3107", "SemanticViolations.RawSqlRepository.EfRawSqlAsync", location, "2099-01-01"));
            File.WriteAllText(expired, SuppressionConfiguration(
                "PE3107", "SemanticViolations.RawSqlRepository.EfRawSqlAsync", location, "2000-01-01"));

            var activeResult = await RunChecksAsync(project, active);
            Assert.HasCount(40, activeResult.Diagnostics.Where(diagnostic => DiagnosticCode(diagnostic) == "PE3107"));
            Assert.IsFalse(activeResult.Diagnostics.Any(diagnostic => DiagnosticCode(diagnostic) == "PE7004"));
            Assert.IsFalse(activeResult.Diagnostics.Any(diagnostic =>
                DiagnosticCode(diagnostic) == "PE3107" && DiagnosticMessage(diagnostic).Contains(
                    "RelationalQueryableExtensions.FromSqlRaw", StringComparison.Ordinal)));
            CollectionAssert.AreEqual(new[] { "csharp" }, activeResult.ExecutedChecks);

            var expiredResult = await RunChecksAsync(project, expired);
            Assert.HasCount(41, expiredResult.Diagnostics.Where(diagnostic => DiagnosticCode(diagnostic) == "PE3107"));
            Assert.HasCount(1, expiredResult.Diagnostics.Where(diagnostic => DiagnosticCode(diagnostic) == "PE7004"));
            Assert.IsTrue(expiredResult.Diagnostics.Any(diagnostic =>
                DiagnosticCode(diagnostic) == "PE3107" && DiagnosticMessage(diagnostic).Contains(
                    "RelationalQueryableExtensions.FromSqlRaw", StringComparison.Ordinal)));
            CollectionAssert.AreEqual(new[] { "csharp" }, expiredResult.ExecutedChecks);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public async Task Expired_suppression_for_another_diagnostic_executes_checks_and_restores_that_diagnostic()
    {
        var project = FixtureProject("SemanticViolations", "SemanticViolations.csproj");
        var target = AnalyzeSemanticFixture().Single(diagnostic =>
            diagnostic.Code == "PE3103" &&
            diagnostic.Message.Contains("SearchMixedStoredProcedureAndEf", StringComparison.Ordinal));
        var directory = Path.Combine(Path.GetTempPath(), $"paradigm-expired-suppression-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var configuration = Path.Combine(directory, "expired.json");
            File.WriteAllText(configuration, SuppressionConfiguration(
                "PE3103",
                "SemanticViolations.InMemoryOrderRepository.SearchMixedStoredProcedureAndEf",
                Path.GetFileName(target.Location!),
                "2000-01-01"));

            var result = await RunChecksAsync(project, configuration);

            Assert.HasCount(1, result.Diagnostics.Where(diagnostic => DiagnosticCode(diagnostic) == "PE7004"));
            Assert.IsTrue(result.Diagnostics.Any(diagnostic =>
                DiagnosticCode(diagnostic) == "PE3103" &&
                DiagnosticMessage(diagnostic).Contains("SearchMixedStoredProcedureAndEf", StringComparison.Ordinal)));
            CollectionAssert.AreEqual(new[] { "csharp" }, result.ExecutedChecks);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public async Task Malformed_configuration_blocks_built_in_checks()
    {
        var project = FixtureProject("SemanticViolations", "SemanticViolations.csproj");
        var directory = Path.Combine(Path.GetTempPath(), $"paradigm-invalid-config-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var configuration = Path.Combine(directory, "invalid.json");
            File.WriteAllText(configuration, """
                {
                  "schemaVersion": "1.0",
                  "suppressions": [
                    {
                      "code": "PE3107",
                      "symbol": "SemanticViolations.RawSqlRepository.EfRawSqlAsync",
                      "location": "RawSqlRepository.cs",
                      "expires": "2099-01-01"
                    }
                  ]
                }
                """);

            var result = await RunChecksAsync(project, configuration);

            Assert.HasCount(1, result.Diagnostics.Where(diagnostic => DiagnosticCode(diagnostic) == "PE5001"));
            Assert.IsEmpty(result.ExecutedChecks);
            Assert.IsFalse(result.Diagnostics.Any(diagnostic => DiagnosticCode(diagnostic).StartsWith("PE31", StringComparison.Ordinal)));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public void Evaluated_interceptor_namespaces_are_carried_into_parse_options()
    {
        var project = FixtureProject("GoodPractices", "GoodPractices.csproj");
        var evaluated = EvaluatedProject.Load(project, "net10.0");
        var feature = evaluated.CreateParseOptions().Features.Single(value =>
            value.Key.Equals("InterceptorsNamespaces", StringComparison.Ordinal));
        string[] namespaces = feature.Value.Split(';');
        CollectionAssert.Contains(namespaces, "GoodPractices.Generated");
        CollectionAssert.Contains(namespaces, "GoodPractices.Preview");
        Assert.HasCount(1, namespaces.Where(value => value.Equals("GoodPractices.Generated", StringComparison.Ordinal)));
    }

    #endregion

    #region Private Methods

    private static IReadOnlyList<CSharpCheckDiagnostic> AnalyzeGoodPracticesFixture() => goodPracticesDiagnostics.Value;
    private static IReadOnlyList<CSharpCheckDiagnostic> AnalyzeSemanticFixture() => semanticDiagnostics.Value;
    private static string DiagnosticCode(System.Text.Json.JsonElement diagnostic) =>
        diagnostic.GetProperty("code").GetString()!;
    private static string DiagnosticMessage(System.Text.Json.JsonElement diagnostic) =>
        diagnostic.GetProperty("message").GetString()!;
    private static RepositorySqlDiagnostic ExpectedRepositorySql(
        string member,
        string construct,
        string file,
        int line,
        int column) => new("PE3107", "error", member, construct, file, line, column);

    private static RepositorySqlDiagnostic ParseRepositorySqlDiagnostic(CSharpCheckDiagnostic diagnostic)
    {
        const string messageSeparator = " uses handwritten SQL or raw database command '";
        const string guidanceSeparator = "'. Use EF/LINQ for simple bounded work or a typed stored-procedure wrapper whose SQL lives in the database project.";
        var memberEnd = diagnostic.Message.IndexOf(messageSeparator, StringComparison.Ordinal);
        var constructStart = memberEnd + messageSeparator.Length;
        var constructEnd = diagnostic.Message.IndexOf(guidanceSeparator, constructStart, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, memberEnd, diagnostic.Message);
        Assert.IsGreaterThan(constructStart, constructEnd, diagnostic.Message);

        var location = diagnostic.Location!;
        var coordinateStart = location.LastIndexOf('(');
        var coordinateEnd = location.LastIndexOf(')');
        var coordinates = location[(coordinateStart + 1)..coordinateEnd].Split(',');
        return new(
            diagnostic.Code,
            diagnostic.Severity,
            diagnostic.Message[..memberEnd],
            diagnostic.Message[constructStart..constructEnd],
            Path.GetFileName(location[..coordinateStart]),
            int.Parse(coordinates[0]),
            int.Parse(coordinates[1]));
    }

    private static async Task<CliChecksResult> RunChecksAsync(string project, string configuration)
    {
        using var output = new StringWriter();
        var exit = await TestCliApplication.RunAsync(
            ["checks", "run", "--project", project, "--config", configuration, "--format", "json"],
            output,
            TextWriter.Null);
        Assert.AreEqual(1, exit, output.ToString());
        using var document = System.Text.Json.JsonDocument.Parse(output.ToString());
        var diagnostics = document.RootElement.GetProperty("diagnostics").EnumerateArray()
            .Select(diagnostic => diagnostic.Clone())
            .ToArray();
        var executed = document.RootElement.GetProperty("checks").GetProperty("executed").EnumerateArray()
            .Select(check => check.GetString()!)
            .ToArray();
        return new(exit, diagnostics, executed);
    }

    private static string SuppressionConfiguration(
        string code,
        string symbol,
        string location,
        string expires) => $$"""
        {
          "schemaVersion": "1.0",
          "suppressions": [
            {
              "code": "{{code}}",
              "symbol": "{{symbol}}",
              "location": "{{location}}",
              "reason": "Focused suppression policy fixture.",
              "expires": "{{expires}}"
            }
          ]
        }
        """;

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
