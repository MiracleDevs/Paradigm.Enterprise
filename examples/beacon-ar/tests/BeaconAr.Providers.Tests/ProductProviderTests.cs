using System.Data;
using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.Operations;
using BeaconAr.Domain.Operations.Repositories;
using BeaconAr.Domain.Receivables.Generated;
using BeaconAr.Providers.MasterData;
using Paradigm.Enterprise.Domain.Uow;
using VersionTokenCodec = BeaconAr.Domain.MasterData.Application.VersionTokenCodec;

namespace BeaconAr.Providers.Tests;

[TestClass]
public sealed class ProductProviderTests
{
    #region Nested Types

    private sealed class FakeProductRepository : IProductRepository
    {
        public Product? Current { get; set; }

        public bool Duplicate { get; set; }

        public bool Referenced { get; set; }

        public bool Deleted { get; private set; }

        public Task<Product?> GetForUpdateAsync(int id, CancellationToken cancellationToken) => Task.FromResult(Current);

        public Task<bool> SkuExistsAsync(string sku, int? excludedId, CancellationToken cancellationToken) => Task.FromResult(Duplicate);

        public Task<bool> HasReferencesAsync(int id, CancellationToken cancellationToken) => Task.FromResult(Referenced);

        public void Add(Product product)
        {
            product.Id = 42;
            product.RowVersion = CurrentVersion;
            Current = product;
        }

        public void Delete(Product product) => Deleted = true;

        public void Dispose()
        {
        }
    }

    private sealed class FakeProductViewRepository : IProductViewRepository
    {
        private readonly FakeProductRepository _products;

        public int SearchCalls { get; private set; }

        public FakeProductViewRepository(FakeProductRepository products)
        {
            _products = products;
        }

        public Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(_products.Current is null ? null : ToDto(_products.Current));

        public Task<PageResult<ProductDto>> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken)
        {
            SearchCalls++;
            return Task.FromResult(new PageResult<ProductDto>(Array.Empty<ProductDto>(), request.PageNumber, request.PageSize, 0, 0));
        }

        public void Dispose()
        {
        }

        private static ProductDto ToDto(Product product) => new(
            product.Id, product.Sku, product.Name, product.Category, product.UnitPrice, product.StockQuantity,
            product.ThumbnailUrl, product.IsActive, product.CreatedByUserId, product.CreationDate,
            product.ModifiedByUserId, product.ModificationDate, VersionTokenCodec.Encode(product.RowVersion));
    }

    private sealed class FakeAuditLogRepository : IAuditLogRepository
    {
        public List<AuditLog> Entries { get; } = new();

        public void Add(AuditLog auditLog) => Entries.Add(auditLog);

        public void Dispose()
        {
        }
    }

    private sealed class FakeTransaction : ITransaction
    {
        public bool IsActive { get; private set; } = true;

        public int CommitCalls { get; private set; }

        public int RollbackCalls { get; private set; }

        public void Commit()
        {
            CommitCalls++;
            IsActive = false;
        }

        public void Rollback()
        {
            RollbackCalls++;
            IsActive = false;
        }

        public void AddCommiteable(ICommiteable commiteable)
        {
        }

        public void AddCommand(IDbCommand command)
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public bool HasActiveTransaction => Transaction.IsActive;

        public FakeTransaction Transaction { get; } = new();

        public int SaveCalls { get; private set; }

        public int? ThrowOnSaveCall { get; set; }

        public Task CommitChangesAsync()
        {
            SaveCalls++;
            if (ThrowOnSaveCall == SaveCalls)
                throw new InvalidOperationException("Injected persistence failure.");
            return Task.CompletedTask;
        }

        public ITransaction CreateTransaction() => Transaction;

        public void RegisterCommiteable(ICommiteable commiteable)
        {
        }

        public ITransaction UseCurrentTransaction() => Transaction;

        public void UseTransaction(ITransaction transaction)
        {
        }

        public void UseTransaction(IDbCommand command)
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakeOperationContext : IApplicationOperationContext
    {
        public int UserId => 9;

        public string CorrelationId => "test-correlation";
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => FixedTime;
    }

    private sealed class FakeClassifier : IMasterDataPersistenceErrorClassifier
    {
        public PersistenceConflictKind Classify(Exception exception) => PersistenceConflictKind.None;
    }

    private sealed class FakePersistenceSession : IPersistenceSession
    {
        public int DiscardCalls { get; private set; }

        public void DiscardTrackedChanges() => DiscardCalls++;
    }

    #endregion

    #region Fields

    private static readonly byte[] CurrentVersion = [1, 2, 3, 4, 5, 6, 7, 8];
    private static readonly DateTimeOffset FixedTime = new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);

    #endregion

    #region Public Methods

    [TestMethod]
    public async Task CreateSavesResourceAndAuditInOneTransaction()
    {
        var repository = new FakeProductRepository();
        var views = new FakeProductViewRepository(repository);
        var audits = new FakeAuditLogRepository();
        var unitOfWork = new FakeUnitOfWork();
        ProductProvider provider = CreateProvider(repository, views, audits, unitOfWork);

        ProductDto result = await provider.CreateAsync(
            new ProductCreateRequest(" SKU ", " Product ", " Category ", 1m, 0, null), CancellationToken.None);

        Assert.AreEqual(42, result.Id);
        Assert.AreEqual("SKU", result.Sku);
        Assert.AreEqual(2, unitOfWork.SaveCalls);
        Assert.AreEqual(1, unitOfWork.Transaction.CommitCalls);
        Assert.AreEqual(0, unitOfWork.Transaction.RollbackCalls);
        Assert.HasCount(1, audits.Entries);
        Assert.AreEqual("created", audits.Entries[0].Action);
        Assert.AreEqual(9, audits.Entries[0].UserId);
        Assert.AreEqual(FixedTime, audits.Entries[0].RecordedAt);
    }

    [TestMethod]
    public async Task StaleUpdateRollsBackWithoutSaving()
    {
        var repository = new FakeProductRepository
        {
            Current = CreateProduct(),
        };
        var views = new FakeProductViewRepository(repository);
        var audits = new FakeAuditLogRepository();
        var unitOfWork = new FakeUnitOfWork();
        ProductProvider provider = CreateProvider(repository, views, audits, unitOfWork);

        MasterDataException exception = await Assert.ThrowsAsync<MasterDataException>(() => provider.UpdateAsync(
            1, new ProductUpdateRequest("SKU", "Product", "Category", 2m, 1, null, true),
            VersionTokenCodec.Encode(new byte[8]), CancellationToken.None));

        Assert.AreEqual("concurrency_conflict", exception.Code);
        Assert.AreEqual(0, unitOfWork.SaveCalls);
        Assert.AreEqual(1, unitOfWork.Transaction.RollbackCalls);
        Assert.HasCount(0, audits.Entries);
    }

    [TestMethod]
    public async Task PersistenceFailureRollsBackAndRemainsUnexpected()
    {
        var repository = new FakeProductRepository();
        var views = new FakeProductViewRepository(repository);
        var audits = new FakeAuditLogRepository();
        var unitOfWork = new FakeUnitOfWork { ThrowOnSaveCall = 1 };
        var persistenceSession = new FakePersistenceSession();
        ProductProvider provider = CreateProvider(repository, views, audits, unitOfWork, persistenceSession);

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.CreateAsync(
            new ProductCreateRequest("SKU", "Product", "Category", 1m, 0, null), CancellationToken.None));

        Assert.AreEqual(1, unitOfWork.Transaction.RollbackCalls);
        Assert.AreEqual(0, unitOfWork.Transaction.CommitCalls);
        Assert.HasCount(0, audits.Entries);
        Assert.AreEqual(1, persistenceSession.DiscardCalls);
    }

    [TestMethod]
    public async Task ReferencedDeleteReturnsStableConflictAndRollsBack()
    {
        var repository = new FakeProductRepository { Current = CreateProduct(), Referenced = true };
        var views = new FakeProductViewRepository(repository);
        var unitOfWork = new FakeUnitOfWork();
        ProductProvider provider = CreateProvider(repository, views, new FakeAuditLogRepository(), unitOfWork);

        MasterDataException exception = await Assert.ThrowsAsync<MasterDataException>(() => provider.DeleteAsync(
            1, VersionTokenCodec.Encode(CurrentVersion), CancellationToken.None));

        Assert.AreEqual("referenced_record", exception.Code);
        Assert.IsFalse(repository.Deleted);
        Assert.AreEqual(1, unitOfWork.Transaction.RollbackCalls);
    }

    [TestMethod]
    public async Task InvalidSearchDoesNotCallRepository()
    {
        var repository = new FakeProductRepository();
        var views = new FakeProductViewRepository(repository);
        ProductProvider provider = CreateProvider(repository, views, new FakeAuditLogRepository(), new FakeUnitOfWork());

        await Assert.ThrowsAsync<MasterDataValidationException>(() => provider.SearchAsync(
            new ProductSearchRequest { PageSize = 101 }, CancellationToken.None));

        Assert.AreEqual(0, views.SearchCalls);
    }

    [TestMethod]
    public async Task InvalidSqlPriceShapeDoesNotOpenTransactionOrSave()
    {
        var repository = new FakeProductRepository();
        var unitOfWork = new FakeUnitOfWork();
        ProductProvider provider = CreateProvider(repository, new FakeProductViewRepository(repository),
            new FakeAuditLogRepository(), unitOfWork);

        MasterDataValidationException exception = await Assert.ThrowsAsync<MasterDataValidationException>(() =>
            provider.CreateAsync(new ProductCreateRequest("SKU", "Product", "Category", 1.23456m, 0, null), CancellationToken.None));

        Assert.IsTrue(exception.Errors.ContainsKey("unitPrice"));
        Assert.AreEqual(0, unitOfWork.SaveCalls);
        Assert.AreEqual(0, unitOfWork.Transaction.CommitCalls);
        Assert.AreEqual(0, unitOfWork.Transaction.RollbackCalls);
    }

    [TestMethod]
    public async Task DeactivationUsesSpecificAuditAction()
    {
        var repository = new FakeProductRepository { Current = CreateProduct() };
        var views = new FakeProductViewRepository(repository);
        var audits = new FakeAuditLogRepository();
        ProductProvider provider = CreateProvider(repository, views, audits, new FakeUnitOfWork());

        await provider.UpdateAsync(1, new ProductUpdateRequest(
            "SKU", "Product", "Category", 1m, 0, null, false), VersionTokenCodec.Encode(CurrentVersion), CancellationToken.None);

        Assert.HasCount(1, audits.Entries);
        Assert.AreEqual("deactivated", audits.Entries[0].Action);
        Assert.IsFalse(repository.Current!.IsActive);
    }

    #endregion

    #region Private Methods

    private static ProductProvider CreateProvider(FakeProductRepository repository, FakeProductViewRepository views,
        FakeAuditLogRepository audits, FakeUnitOfWork unitOfWork, FakePersistenceSession? persistenceSession = null) => new(
        repository, views, audits, unitOfWork, new FakeOperationContext(), new FakeTimeProvider(), new FakeClassifier(),
        persistenceSession ?? new FakePersistenceSession());

    private static Product CreateProduct() => new()
    {
        Id = 1,
        Sku = "SKU",
        Name = "Product",
        Category = "Category",
        UnitPrice = 1m,
        IsActive = true,
        CreationDate = FixedTime,
        RowVersion = CurrentVersion.ToArray(),
    };

    #endregion
}
