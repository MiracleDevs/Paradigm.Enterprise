using System.Reflection;
using BeaconAr.Domain.Sales.Entities;
using BeaconAr.Providers.Sales;
using Paradigm.Enterprise.Domain.Repositories;

namespace BeaconAr.Architecture.Tests;

[TestClass]
public sealed class LayerBoundaryTests
{
    #region Public Methods

    [TestMethod]
    public void BusinessSourceUsesOnlyTheFourCapabilityRoots()
    {
        string root = FindExampleRoot();
        foreach (string project in new[] { "BeaconAr.Data", "BeaconAr.Domain", "BeaconAr.Providers" })
        {
            string projectRoot = Path.Combine(root, "src", project);
            string[] forbidden = ["Receivables", "Reporting"];
            foreach (string name in forbidden)
                Assert.IsFalse(Directory.Exists(Path.Combine(projectRoot, name)), $"{project}/{name}");
        }
    }

    [TestMethod]
    public void ProductionRepositoriesContainNoHandwrittenSqlBoundary()
    {
        string dataRoot = Path.Combine(FindExampleRoot(), "src", "BeaconAr.Data");
        string[] forbidden = ["FromSql", "ExecuteSql", "SqlQuery", "CommandText", "\"SELECT ", "\"INSERT ", "\"UPDATE ", "\"DELETE ", "\"MERGE "];
        string[] repositoryFiles = Directory.GetFiles(dataRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}Repositories{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToArray();
        Assert.IsNotEmpty(repositoryFiles);
        foreach (string file in repositoryFiles)
        {
            string source = File.ReadAllText(file);
            foreach (string token in forbidden)
                Assert.IsFalse(source.Contains(token, StringComparison.OrdinalIgnoreCase), $"{file} contains {token}");
        }
    }

    [TestMethod]
    public void RepositoryDiscoverySurfaceIsPublicConcreteAndExactlyNamed()
    {
        Type repositoryMarker = typeof(IRepository);
        Type[] repositories = typeof(BeaconAr.Data.MasterData.Repositories.ProductRepository).Assembly.GetTypes()
            .Where(type => type.IsPublic && !type.IsAbstract && repositoryMarker.IsAssignableFrom(type))
            .ToArray();
        Assert.IsNotEmpty(repositories);
        foreach (Type repository in repositories)
            Assert.AreEqual(1, repository.GetInterfaces().Count(contract => contract.Name == $"I{repository.Name}"), repository.FullName);
    }

    [TestMethod]
    public void GeneratedMapperOutputIsCapabilityOwned()
    {
        string dataRoot = Path.Combine(FindExampleRoot(), "src", "BeaconAr.Data");
        Assert.IsFalse(Directory.Exists(Path.Combine(dataRoot, "Mappers")));
        foreach (string capability in new[] { "Access", "MasterData", "Operations", "Sales" })
        {
            string mapperRoot = Path.Combine(dataRoot, capability, "Mappers");
            Assert.IsTrue(File.Exists(Path.Combine(mapperRoot, $"{capability}StoredProcedureMappersRegisterer.cs")));
        }
    }

    [TestMethod]
    public void DatabaseContainsExactlyThePlannedNewRoutineSet()
    {
        string routines = Path.Combine(FindExampleRoot(), "src", "database", "routines");
        string[] expected =
        [
            "Access/GetOrCreateApplicationUser.sql",
            "MasterData/HasAddressReferences.sql", "MasterData/HasCarrierReferences.sql", "MasterData/HasCustomerReferences.sql",
            "MasterData/HasProductReferences.sql", "MasterData/LockCustomerForAddressMutation.sql", "MasterData/SearchAddress.sql",
            "MasterData/SearchCarrier.sql", "MasterData/SearchCustomer.sql", "MasterData/SearchProduct.sql",
            "Operations/GetDashboardSummary.sql", "Operations/LockIdempotencyRequest.sql",
            "Sales/AllocateQuoteNumber.sql", "Sales/AllocateSalesOrderNumber.sql", "Sales/GetAddressSalesReferenceForUpdate.sql",
            "Sales/GetCarrierSalesReferenceForUpdate.sql", "Sales/GetCustomerSalesReferenceForUpdate.sql",
            "Sales/GetProductSalesReferenceForUpdate.sql", "Sales/LockQuoteForConversion.sql", "Sales/SearchQuote.sql",
            "Sales/SearchSalesOrder.sql",
        ];
        string[] actual = Directory.GetFiles(routines, "*.sql", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(routines, path).Replace('\\', '/')).Order(StringComparer.Ordinal).ToArray();
        CollectionAssert.AreEqual(expected.Order(StringComparer.Ordinal).ToArray(), actual);
        Assert.IsFalse(Directory.Exists(Path.Combine(routines, "Reporting")));
    }

    [TestMethod]
    public void DomainHasNoBroadStaticValidationOwners()
    {
        string domainRoot = Path.Combine(FindExampleRoot(), "src", "BeaconAr.Domain");
        string[] offenders = Directory.GetFiles(domainRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => Path.GetFileNameWithoutExtension(path).EndsWith("RequestValidator", StringComparison.Ordinal) ||
                           Path.GetFileNameWithoutExtension(path).EndsWith("DomainValidation", StringComparison.Ordinal))
            .ToArray();

        Assert.IsEmpty(offenders, string.Join(Environment.NewLine, offenders));
    }

    [TestMethod]
    public void HandwrittenEntityPartialsAreCoLocatedAndNamespaceAligned()
    {
        string domainRoot = Path.Combine(FindExampleRoot(), "src", "BeaconAr.Domain");
        foreach (string behavior in Directory.GetFiles(domainRoot, "*.Behavior.cs", SearchOption.AllDirectories))
        {
            string generated = behavior[..^".Behavior.cs".Length] + ".cs";
            Assert.IsTrue(File.Exists(generated), $"Generated partner is missing for {behavior}.");
            string relativeDirectory = Path.GetRelativePath(domainRoot, Path.GetDirectoryName(behavior)!)
                .Replace(Path.DirectorySeparatorChar, '.');
            string expectedNamespace = $"BeaconAr.Domain.{relativeDirectory}";
            StringAssert.Contains(File.ReadAllText(behavior), $"namespace {expectedNamespace};", behavior);
            StringAssert.Contains(File.ReadAllText(generated), $"namespace {expectedNamespace};", generated);
        }
    }

    [TestMethod]
    public void ManualPublicReadDtosMatchTheReviewedCompatibilityAllowList()
    {
        string contractsRoot = Path.Combine(FindExampleRoot(), "src", "BeaconAr.Domain");
        string[] actual = Directory.GetFiles(contractsRoot, "*Dto.cs", SearchOption.AllDirectories)
            .Select(path => Path.GetFileNameWithoutExtension(path)!).Order(StringComparer.Ordinal).ToArray();
        string[] expected =
        [
            "CurrentUserDto", "DashboardSummaryDto", "QuoteDto", "SalesLineDto", "SalesOrderDto",
        ];

        CollectionAssert.AreEqual(expected.Order(StringComparer.Ordinal).ToArray(), actual);
    }

    [TestMethod]
    public void SalesSearchContractsReturnCanonicalGeneratedViews()
    {
        Type quoteResult = typeof(IQuoteProvider).GetMethod(nameof(IQuoteProvider.SearchAsync))!.ReturnType;
        Type orderResult = typeof(ISalesOrderProvider).GetMethod(nameof(ISalesOrderProvider.SearchAsync))!.ReturnType;

        Assert.AreEqual(typeof(QuoteView), quoteResult.GenericTypeArguments[0].GenericTypeArguments[0]);
        Assert.AreEqual(typeof(SalesOrderView), orderResult.GenericTypeArguments[0].GenericTypeArguments[0]);
    }

    [TestMethod]
    public void PublicWriteEndpointsDoNotAcceptPersistenceEntitiesOrViews()
    {
        string controllerRoot = Path.Combine(FindExampleRoot(), "src", "BeaconAr.WebApi", "Controllers");
        string source = string.Join(Environment.NewLine,
            Directory.GetFiles(controllerRoot, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
        Type[] persistenceTypes = typeof(Quote).Assembly.GetTypes()
            .Where(type => type.IsPublic && type.Namespace?.Contains(".Entities", StringComparison.Ordinal) == true)
            .ToArray();

        foreach (Type persistenceType in persistenceTypes)
            Assert.IsFalse(source.Contains($"[FromBody] {persistenceType.Name} ", StringComparison.Ordinal),
                $"A write endpoint accepts persistence type {persistenceType.FullName}.");
    }

    #endregion

    #region Private Methods

    private static string FindExampleRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "BeaconAr.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Beacon AR example root was not found.");
    }

    #endregion
}
