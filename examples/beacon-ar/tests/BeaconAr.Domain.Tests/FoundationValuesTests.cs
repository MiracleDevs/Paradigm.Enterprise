using BeaconAr.Domain.MasterData;
using BeaconAr.Domain.Operations;
using BeaconAr.Domain.Sales;

namespace BeaconAr.Domain.Tests;

[TestClass]
public sealed class FoundationValuesTests
{
    #region Fields

    private static readonly int[] IdempotencyStateIds = [1, 2, 3];
    private static readonly int[] QuoteStatusIds = [1, 2, 3, 4, 5];
    private static readonly int[] SalesOrderStatusIds = [1, 2, 3, 4, 5, 6];

    #endregion

    #region Public Methods

    [TestMethod]
    public void CatalogIdentifiersMatchPublishedAssignments()
    {
        CollectionAssert.AreEqual(IdempotencyStateIds, Enum.GetValues<IdempotencyState>().Select(static value => (int)value).ToArray());
        CollectionAssert.AreEqual(QuoteStatusIds, Enum.GetValues<QuoteStatus>().Select(static value => (int)value).ToArray());
        CollectionAssert.AreEqual(SalesOrderStatusIds, Enum.GetValues<SalesOrderStatus>().Select(static value => (int)value).ToArray());

        var root = FindExampleRoot();
        StringAssert.Contains(File.ReadAllText(Path.Combine(root, "src", "database", "scripts", "postdeployment", "MasterData", "AddressTypeData.sql")), "(3, N'both'");
        StringAssert.Contains(File.ReadAllText(Path.Combine(root, "src", "database", "scripts", "postdeployment", "Operations", "IdempotencyStateData.sql")), "(3, N'failed'");
        StringAssert.Contains(File.ReadAllText(Path.Combine(root, "src", "database", "scripts", "postdeployment", "Sales", "QuoteStatusData.sql")), "(5, N'expired'");
        StringAssert.Contains(File.ReadAllText(Path.Combine(root, "src", "database", "scripts", "postdeployment", "Sales", "SalesOrderStatusData.sql")), "(6, N'cancelled'");
    }

    [TestMethod]
    [DataRow("1.004", "1.00")]
    [DataRow("1.005", "1.01")]
    [DataRow("1.006", "1.01")]
    [DataRow("-1.005", "-1.01")]
    public void MonetaryRoundingUsesTwoDigitMidpointAwayFromZero(string value, string expected)
    {
        Assert.AreEqual(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), MonetaryRounding.Round(decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture)));
    }

    #endregion

    #region Private Methods

    private static string FindExampleRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "aspire.config.json")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Beacon AR example root not found.");
    }

    #endregion
}
