namespace Paradigm.Enterprise.Cli.Tests;
[TestClass]
public class AnalysisTests
{
#region Public Methods
    [TestMethod]
    public void Provider_without_convention_interface_is_an_error()
    {
        var provider = Type("Sample.OrdersProvider", "OrdersProvider", ["Paradigm.Enterprise.Providers.IProvider"]);
        var diagnostics = Analysis.ValidateTypes([provider]);
        Assert.HasCount(1, diagnostics);
        var diagnostic = diagnostics[0];
        Assert.AreEqual("PE3001", diagnostic.Code);
        Assert.AreEqual("error", diagnostic.Severity);
    }

    [TestMethod]
    public void Paradigm_controller_without_independent_filter_is_a_warning()
    {
        var controller = Type("Sample.OrdersController", "OrdersController", [], "Paradigm.Enterprise.WebApi.Controllers.ApiControllerBase");
        var diagnostics = Analysis.ValidateTypes([controller]);
        Assert.HasCount(1, diagnostics);
        var diagnostic = diagnostics[0];
        Assert.AreEqual("PE4001", diagnostic.Code);
        Assert.AreEqual("warning", diagnostic.Severity);
    }

    [TestMethod]
    public void Api_authorization_filter_satisfies_anonymous_base_guard()
    {
        var controller = Type("Sample.StatusController", "StatusController", [], "Paradigm.Enterprise.WebApi.Controllers.ApiControllerBase", ["Paradigm.Enterprise.WebApi.Attributes.ApiAuthorizationAttribute"]);
        Assert.IsEmpty(Analysis.ValidateTypes([controller]));
    }

    [TestMethod]
    public void Authorization_on_intermediate_controller_base_is_discovered()
    {
        var frameworkBase = Type("Paradigm.Enterprise.WebApi.Controllers.ApiControllerBase", "ApiControllerBase", [], "Microsoft.AspNetCore.Mvc.ControllerBase", ["Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute"], isAbstract: true);
        var securedBase = Type("Sample.SecuredControllerBase", "SecuredControllerBase", [], frameworkBase.FullName, ["Paradigm.Enterprise.WebApi.Attributes.ApiAuthorizationAttribute"], isAbstract: true);
        var controller = Type("Sample.OrdersController", "OrdersController", [], securedBase.FullName);
        Assert.IsEmpty(Analysis.ValidateTypes([controller], [controller, securedBase, frameworkBase]));
    }

    [TestMethod]
    public void Unprotected_action_on_intermediate_anonymous_base_is_reported()
    {
        var frameworkBase = Type("Paradigm.Enterprise.WebApi.Controllers.ApiControllerBase", "ApiControllerBase", [], "Microsoft.AspNetCore.Mvc.ControllerBase", ["Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute"], [new("Get", "public Task Get()", [])], isAbstract: true);
        var intermediate = Type("Sample.OrdersControllerBase", "OrdersControllerBase", [], frameworkBase.FullName, actions: [new("Post", "public Task Post()", ["Paradigm.Enterprise.WebApi.Attributes.ApiAuthorizationAttribute"])], isAbstract: true);
        var controller = Type("Sample.OrdersController", "OrdersController", [], intermediate.FullName);
        var diagnostics = Analysis.ValidateTypes([controller], [controller, intermediate, frameworkBase]);
        Assert.IsTrue(diagnostics.Any(x => x.Code == "PE4001"));
    }

    [TestMethod]
    public void Capability_with_mixed_identifier_types_is_an_error()
    {
        var repository = Type("Sample.OrderRepository", "OrderRepository", ["Sample.IOrderRepository", "Paradigm.Enterprise.Domain.Repositories.IRepository", "IReadRepository<Sample.Order, System.Int32>"]);
        var provider = Type("Sample.OrderProvider", "OrderProvider", ["Sample.IOrderProvider", "Paradigm.Enterprise.Providers.IProvider", "IReadProvider<Sample.OrderView, System.Guid>"]);
        var diagnostics = Analysis.ValidateTypes([repository, provider]);
        Assert.IsTrue(diagnostics.Any(x => x.Code == "PE3002"));
    }

    [TestMethod]
    public void Identifier_validation_includes_entity_view_and_controller()
    {
        var entity = Type("Sample.Order", "Order", ["Paradigm.Enterprise.Interfaces.IEntity<System.Guid>"]);
        var view = Type("Sample.OrderView", "OrderView", ["Paradigm.Enterprise.Interfaces.IEntity<System.Guid>"]);
        var controller = Type("Sample.OrderController", "OrderController", [], "Paradigm.Enterprise.WebApi.Controllers.ReadApiControllerBase<Sample.IOrderProvider, Sample.OrderView, Sample.Parameters, System.Int32>");
        var diagnostics = Analysis.ValidateTypes([entity, view, controller]);
        Assert.IsTrue(diagnostics.Any(x => x.Code == "PE3002"));
    }

#endregion
#region Private Methods
    private static InspectedType Type(string fullName, string name, IReadOnlyList<string> interfaces, string? baseType = null, IReadOnlyList<string>? attributes = null, IReadOnlyList<InspectedAction>? actions = null, bool isAbstract = false) => new(fullName, name, "Sample", baseType, interfaces, attributes ?? [], [], actions ?? [], true, isAbstract, "Sample", null, null);
#endregion
}
