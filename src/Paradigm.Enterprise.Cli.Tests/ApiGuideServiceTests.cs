namespace Paradigm.Enterprise.Cli.Tests;

[TestClass]
public class ApiGuideServiceTests
{
    #region Public Methods

    [TestMethod]
    public void MySql_routine_guide_describes_the_provider_and_mapper_boundary()
    {
        var type = new InspectedType(
            "Paradigm.Enterprise.Data.MySql.StoredProcedures.ResultStoredProcedureBase<TParameters, TResult1>",
            "ResultStoredProcedureBase<TParameters, TResult1>",
            "Paradigm.Enterprise.Data.MySql.StoredProcedures",
            null, [], [], [], [], true, true, "Paradigm.Enterprise.Data.MySql", null, null);

        var guide = new ApiGuideService().Create(type);

        Assert.IsNotNull(guide);
        StringAssert.Contains(guide.RecommendedPattern, "MySQL");
        Assert.IsTrue(guide.DiscoveryAndRegistration.Any(item =>
            item.Contains("Oracle MySql.Data", StringComparison.Ordinal) &&
            item.Contains("MySqlParameterMapperFactory", StringComparison.Ordinal)));
        Assert.IsTrue(guide.Verification.Any(item => item.Contains("--package MySql", StringComparison.Ordinal)));
        Assert.IsTrue(guide.Cautions.Any(item => item.Contains("do not accept cancellation tokens", StringComparison.Ordinal)));
    }

    #endregion
}