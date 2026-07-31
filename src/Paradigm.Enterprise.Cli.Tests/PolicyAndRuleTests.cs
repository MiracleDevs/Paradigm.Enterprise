namespace Paradigm.Enterprise.Cli.Tests;
[TestClass]
public class PolicyAndRuleTests
{
#region Public Methods
    [TestMethod]
    public void Public_entity_setter_is_warning_but_view_and_identity_are_allowed()
    {
        var entity = Type("Sample.Product", "Product", "entity", [Property("Id", "System.Int32", "public"), Property("Name", "System.String", "public")]);
        var view = Type("Sample.ProductView", "ProductView", "view", [Property("Name", "System.String", "public")]);
        var diagnostics = Analysis.ValidateTypes([entity, view]);
        var warning = diagnostics.Single(x => x.Code == "PE3101");
        Assert.AreEqual("warning", warning.Severity);
        StringAssert.Contains(warning.Message, ".Name");
    }

    [TestMethod]
    public void Non_public_entity_setter_is_allowed()
    {
        var entity = Type("Sample.Product", "Product", "entity", [Property("Name", "System.String", "private")]);
        Assert.IsFalse(Analysis.ValidateTypes([entity]).Any(x => x.Code == "PE3101"));
    }

    [TestMethod]
    public void Generated_type_and_generated_property_setters_are_allowed()
    {
        var generatedType = Type("Sample.GeneratedEntity", "GeneratedEntity", "entity", [Property("Name", "System.String", "public")])with
        {
            Attributes = ["System.CodeDom.Compiler.GeneratedCodeAttribute"]
        };
        var generatedProperty = Property("PersistenceValue", "System.String", "public")with
        {
            Attributes = ["System.Runtime.CompilerServices.CompilerGeneratedAttribute"]
        };
        var entity = Type("Sample.Product", "Product", "entity", [generatedProperty, Property("Name", "System.String", "private")]);
        var diagnostics = Analysis.ValidateTypes([generatedType, entity]);
        Assert.IsFalse(diagnostics.Any(x => x.Code == "PE3101"));
    }

    [TestMethod]
    public void Reviewed_unexpired_PE3101_suppression_matches_the_configured_symbol()
    {
        var diagnostic = new Diagnostic("PE3101", "warning", "Sample.Product.Name has a public setter.", "Sample:Sample.Product.Name");
        var suppression = new ConfiguredSuppression("PE3101", "Sample.Product.Name", null, "generated persistence model", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)));
        Assert.IsTrue(SuppressionPolicy.IsSuppressed(diagnostic, [suppression]));
        Assert.IsFalse(SuppressionPolicy.IsSuppressed(diagnostic, [suppression with { Expires = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)) }]));
    }

    [TestMethod]
    public void Public_repository_iqueryable_is_error()
    {
        var repository = Type("Sample.IProductRepository", "IProductRepository", "repository", [new("method", "Query", "public abstract System.Linq.IQueryable<Sample.Product> Query()", "System.Linq.IQueryable<Sample.Product>", [], [], [])], isAbstract: true);
        var diagnostic = Analysis.ValidateTypes([repository]).Single(x => x.Code == "PE3102");
        Assert.AreEqual("error", diagnostic.Severity);
    }

    [TestMethod]
    public void Protected_repository_iqueryable_hook_is_allowed()
    {
        var repository = Type("Sample.ProductRepository", "ProductRepository", "repository", [new("method", "AsQueryable", "protected override System.Linq.IQueryable<Sample.Product> AsQueryable()", "System.Linq.IQueryable<Sample.Product>", [], [], [])]);
        Assert.IsFalse(Analysis.ValidateTypes([repository]).Any(x => x.Code == "PE3102"));
    }

    [TestMethod]
    public void Task_wrapped_iqueryable_is_rejected_but_protected_and_materialized_results_are_allowed()
    {
        var repository = Type("Sample.ProductRepository", "ProductRepository", "repository", [new("method", "QueryAsync", "public System.Threading.Tasks.Task<System.Linq.IQueryable<Sample.Product>> QueryAsync()", "System.Threading.Tasks.Task<System.Linq.IQueryable<Sample.Product>>", [], [], []), new("method", "ComposeAsync", "protected System.Threading.Tasks.Task<System.Linq.IQueryable<Sample.Product>> ComposeAsync()", "System.Threading.Tasks.Task<System.Linq.IQueryable<Sample.Product>>", [], [], []), new("method", "ListAsync", "public System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<Sample.Product>> ListAsync()", "System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<Sample.Product>>", [], [], [])]);
        var diagnostics = Analysis.ValidateTypes([repository]).Where(x => x.Code == "PE3102").ToArray();
        Assert.HasCount(1, diagnostics);
        StringAssert.Contains(diagnostics[0].Message, ".QueryAsync");
    }

    [TestMethod]
    public void PE3102_suppression_is_symbol_specific_and_expiry_is_deterministic()
    {
        var diagnostic = new Diagnostic("PE3102", "error", "Sample.ProductRepository.QueryAsync exposes IQueryable across the repository boundary.", "Sample:Sample.ProductRepository.QueryAsync");
        var suppression = new ConfiguredSuppression("PE3102", "Sample.ProductRepository.QueryAsync", null, "reviewed compatibility boundary", new DateOnly(2099, 1, 1));
        Assert.IsTrue(SuppressionPolicy.IsSuppressed(diagnostic, [suppression]));
        Assert.IsFalse(SuppressionPolicy.IsSuppressed(diagnostic, [suppression with { Symbol = "Sample.OtherRepository.QueryAsync" }]));
        Assert.IsFalse(SuppressionPolicy.IsSuppressed(diagnostic, [suppression with { Expires = new DateOnly(2000, 1, 1) }]));
    }

    [TestMethod]
    public void Package_policy_reports_mixed_framework_versions_and_expired_suppression()
    {
        var configuration = new ParadigmConfiguration("config.json", ".", new(), [new("PE9999", "Sample", null, "temporary", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)))]);
        var packages = new[]
        {
            new PackageInfo("Paradigm.Enterprise.Domain", "1.0.0", "App.csproj"),
            new PackageInfo("Paradigm.Enterprise.Data", "2.0.0", "App.csproj")
        };
        var diagnostics = new PackagePolicyService().Check(packages, [], configuration);
        Assert.IsTrue(diagnostics.Any(x => x.Code == "PE1001"));
        Assert.IsTrue(diagnostics.Any(x => x.Code == "PE7004"));
    }

#endregion
#region Private Methods
    private static ApiMember Property(string name, string type, string setterVisibility) => new("property", name, $"public {type} {name} {{ get; set; }}", type, [new("get", "public"), new("set", setterVisibility)], [], []);
    private static InspectedType Type(string fullName, string name, string kind, IReadOnlyList<ApiMember> members, bool isAbstract = false)
    {
        IReadOnlyList<string> interfaces = kind switch
        {
            "entity" or "view" => ["Paradigm.Enterprise.Interfaces.IEntity<System.Int32>"],
            "repository" => ["Paradigm.Enterprise.Domain.Repositories.IRepository"],
            _ => []
        };
        return new(fullName, name, "Sample", null, interfaces, [], members.Select(x => x.Signature).ToArray(), [], true, isAbstract, "Sample", null, null, members);
    }
#endregion
}
