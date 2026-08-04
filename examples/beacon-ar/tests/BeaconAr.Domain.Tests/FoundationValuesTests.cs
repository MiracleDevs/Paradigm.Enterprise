using BeaconAr.Domain.Sales;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using AddressType = BeaconAr.Interfaces.MasterData.Enums.AddressType;
using IdempotencyState = BeaconAr.Interfaces.Operations.Enums.IdempotencyState;
using QuoteStatus = BeaconAr.Interfaces.Sales.Enums.QuoteStatus;
using SalesOrderStatus = BeaconAr.Interfaces.Sales.Enums.SalesOrderStatus;

namespace BeaconAr.Domain.Tests;

[TestClass]
public sealed class FoundationValuesTests
{
    #region Public Methods

    [TestMethod]
    public void CatalogIdentifiersMatchPublishedAssignments()
    {
        var root = FindExampleRoot();
        AssertCatalogParity<AddressType>(Path.Combine(root, "src", "database", "scripts", "postdeployment", "MasterData", "AddressTypeData.sql"));
        AssertCatalogParity<IdempotencyState>(Path.Combine(root, "src", "database", "scripts", "postdeployment", "Operations", "IdempotencyStateData.sql"));
        AssertCatalogParity<QuoteStatus>(Path.Combine(root, "src", "database", "scripts", "postdeployment", "Sales", "QuoteStatusData.sql"));
        AssertCatalogParity<SalesOrderStatus>(Path.Combine(root, "src", "database", "scripts", "postdeployment", "Sales", "SalesOrderStatusData.sql"));

        Assert.AreEqual("BeaconAr.Interfaces", typeof(AddressType).Assembly.GetName().Name);
        Assert.AreEqual("BeaconAr.Interfaces", typeof(IdempotencyState).Assembly.GetName().Name);
        Assert.AreEqual("BeaconAr.Interfaces", typeof(QuoteStatus).Assembly.GetName().Name);
        Assert.AreEqual("BeaconAr.Interfaces", typeof(SalesOrderStatus).Assembly.GetName().Name);
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

    private static void AssertCatalogParity<TEnum>(string seedPath) where TEnum : struct, Enum
    {
        string[] seeded = Regex.Matches(File.ReadAllText(seedPath), @"\((\d+),\s*N'([^']+)'", RegexOptions.CultureInvariant)
            .Select(match => $"{match.Groups[1].Value}:{match.Groups[2].Value}")
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        string[] declared = Enum.GetValues<TEnum>()
            .Select(value =>
            {
                FieldInfo field = typeof(TEnum).GetField(value.ToString())!;
                string code = field.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? string.Empty;
                return $"{Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture)}:{code}";
            })
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        CollectionAssert.AreEqual(seeded, declared, $"Seed parity failed for {typeof(TEnum).FullName}.");
    }

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
