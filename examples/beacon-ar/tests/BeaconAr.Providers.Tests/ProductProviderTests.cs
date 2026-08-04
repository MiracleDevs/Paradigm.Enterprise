using System.Data;
using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.Operations;
using BeaconAr.Domain.Operations.Repositories;
using BeaconAr.Domain.Access.Entities;
using BeaconAr.Domain.MasterData.Entities;
using BeaconAr.Domain.Operations.Entities;
using BeaconAr.Domain.Sales.Entities;
using BeaconAr.Providers.MasterData;
using Paradigm.Enterprise.Domain.Dtos;
using Paradigm.Enterprise.Domain.Uow;
using VersionTokenCodec = BeaconAr.Domain.Operations.VersionTokenCodec;
using Paradigm.Enterprise.Domain.Exceptions;

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

        public Task<Product> AddAsync(Product product)
        {
            product.Id = 42;
            product.RowVersion = CurrentVersion;
            Current = product;
            return Task.FromResult(product);
        }

        public Task AddAsync(IEnumerable<Product> products) => Task.WhenAll(products.Select(AddAsync));

        public Task<Product> UpdateAsync(Product product) => Task.FromResult(product);

        public Task UpdateAsync(IEnumerable<Product> products) => Task.CompletedTask;

        public Task DeleteAsync(Product product)
        {
            Deleted = true;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(IEnumerable<Product> products)
        {
            Deleted = true;
            return Task.CompletedTask;
        }

        public Task<Product?> GetByIdAsync(int id) => Task.FromResult(Current);

        public Task<IEnumerable<Product>> GetByIdsAsync(IEnumerable<int> ids) =>
            Task.FromResult<IEnumerable<Product>>(Current is null ? [] : [Current]);

        public Task<IEnumerable<Product>> GetAllAsync() =>
            Task.FromResult<IEnumerable<Product>>(Current is null ? [] : [Current]);

        public Task<PaginatedResultDto<Product>> SearchAsync<TParameters>(TParameters parameters)
            where TParameters : PaginationParametersBase => throw new NotSupportedException();

        public Task<PaginatedResultDto<Product>> SearchPaginatedAsync(FilterTextPaginatedParameters parameters) =>
            throw new NotSupportedException();

        public void Dispose()
        {
        }
    }

    private sealed class FakeProductViewRepository : IProductViewRepository
    {
        private readonly FakeProductRepository _products;

        public int SearchCalls { get; private set; }

        public CancellationToken LastIdentifierCancellationToken { get; private set; }

        public FakeProductViewRepository(FakeProductRepository products)
        {
            _products = products;
        }

        public Task<ProductView?> GetByIdAsync(int id) =>
            Task.FromResult(_products.Current is null ? null : ToView(_products.Current));

        public Task<ProductView?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            LastIdentifierCancellationToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return GetByIdAsync(id);
        }

        public Task<PageResult<ProductView>> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken)
        {
            SearchCalls++;
            return Task.FromResult(new PageResult<ProductView>(Array.Empty<ProductView>(), request.PageNumber, request.PageSize, 0, 0));
        }

        public Task<IEnumerable<ProductView>> GetByIdsAsync(IEnumerable<int> ids) =>
            Task.FromResult<IEnumerable<ProductView>>(_products.Current is null ? [] : [ToView(_products.Current)]);

        public Task<IEnumerable<ProductView>> GetAllAsync() =>
            Task.FromResult<IEnumerable<ProductView>>(_products.Current is null ? [] : [ToView(_products.Current)]);

        public Task<PaginatedResultDto<ProductView>> SearchAsync<TParameters>(TParameters parameters)
            where TParameters : PaginationParametersBase
        {
            SearchCalls++;
            ProductView[] results = _products.Current is null ? [] : [ToView(_products.Current)];
            return Task.FromResult(new PaginatedResultDto<ProductView>(new PaginationInfo
            {
                ItemsCount = results.Length,
                PageNumber = parameters.PageNumber ?? 1,
                TotalPages = results.Length,
            }, results));
        }

        public Task<PaginatedResultDto<ProductView>> SearchPaginatedAsync(FilterTextPaginatedParameters parameters) =>
            throw new NotSupportedException();

        public void Dispose()
        {
        }

        private static ProductView ToView(Product product) => new()
        {
            Id = product.Id,
            Sku = product.Sku,
            Name = product.Name,
            Category = product.Category,
            UnitPrice = product.UnitPrice,
            StockQuantity = product.StockQuantity,
            ThumbnailUrl = product.ThumbnailUrl,
            IsActive = product.IsActive,
            CreatedByUserId = product.CreatedByUserId,
            CreationDate = product.CreationDate,
            ModifiedByUserId = product.ModifiedByUserId,
            ModificationDate = product.ModificationDate,
            RowVersion = product.RowVersion,
        };
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
        public bool IsActive { get; private set; }

        public int CommitCalls { get; private set; }

        public int RollbackCalls { get; private set; }

        public void Activate() => IsActive = true;

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

        public ITransaction CreateTransaction()
        {
            Transaction.Activate();
            return Transaction;
        }

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

        ProductView result = await provider.CreateAsync(
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

        await Assert.ThrowsAsync<DomainException>(() =>
            provider.CreateAsync(new ProductCreateRequest("SKU", "Product", "Category", 1.23456m, 0, null), CancellationToken.None));

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

    [TestMethod]
    public async Task LegacyIdentifierReadPropagatesCancellationToken()
    {
        var repository = new FakeProductRepository { Current = CreateProduct() };
        var views = new FakeProductViewRepository(repository);
        ProductProvider provider = CreateProvider(repository, views, new FakeAuditLogRepository(), new FakeUnitOfWork());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => provider.GetByIdAsync(1, cancellation.Token));

        Assert.AreEqual(cancellation.Token, views.LastIdentifierCancellationToken);
    }

    #endregion

    #region Private Methods

    private static ProductProvider CreateProvider(FakeProductRepository repository, FakeProductViewRepository views,
        FakeAuditLogRepository audits, FakeUnitOfWork unitOfWork, FakePersistenceSession? persistenceSession = null)
    {
        var coordinator = new MasterDataMutationCoordinator(audits, unitOfWork, new FakeOperationContext(),
            new FakeTimeProvider(), new FakeClassifier(), persistenceSession ?? new FakePersistenceSession());
        return new ProductProvider(repository, views, unitOfWork, coordinator);
    }

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
