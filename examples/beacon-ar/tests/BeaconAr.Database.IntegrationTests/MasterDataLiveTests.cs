using BeaconAr.Data.MasterData;
using BeaconAr.Data.Operations;
using BeaconAr.Data.Receivables.Context;
using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.Operations;
using BeaconAr.Domain.Operations.Repositories;
using BeaconAr.Domain.Receivables.Entities;
using BeaconAr.Providers.MasterData;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Data.Uow;
using Paradigm.Enterprise.Domain.Dtos;
using Paradigm.Enterprise.Domain.Extensions;
using Paradigm.Enterprise.Domain.Uow;

namespace BeaconAr.Database.IntegrationTests;

[TestClass]
[TestCategory("Integration")]
public sealed class MasterDataLiveTests
{
    #region Nested Types

    private sealed class TestOperationContext(int userId, string correlationId) : IApplicationOperationContext
    {
        public int UserId { get; } = userId;

        public string CorrelationId { get; } = correlationId;
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }

    private sealed class SkuRaceGate
    {
        private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _remaining = 2;

        public async Task WaitAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Decrement(ref _remaining) == 0)
                _ready.TrySetResult();

            await _ready.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class CoordinatedProductRepository(ProductRepository inner, SkuRaceGate gate) : IProductRepository
    {
        public Task<Product?> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
            inner.GetForUpdateAsync(id, cancellationToken);

        public async Task<bool> SkuExistsAsync(string sku, int? excludedId, CancellationToken cancellationToken)
        {
            bool exists = await inner.SkuExistsAsync(sku, excludedId, cancellationToken);
            if (!exists && !excludedId.HasValue)
                await gate.WaitAsync(cancellationToken);
            return exists;
        }

        public Task<bool> HasReferencesAsync(int id, CancellationToken cancellationToken) =>
            inner.HasReferencesAsync(id, cancellationToken);

        public Task<Product> AddAsync(Product product) => inner.AddAsync(product);

        public Task AddAsync(IEnumerable<Product> products) => inner.AddAsync(products);

        public Task<Product> UpdateAsync(Product product) => inner.UpdateAsync(product);

        public Task UpdateAsync(IEnumerable<Product> products) => inner.UpdateAsync(products);

        public Task DeleteAsync(Product product) => inner.DeleteAsync(product);

        public Task DeleteAsync(IEnumerable<Product> products) => inner.DeleteAsync(products);

        public Task<Product?> GetByIdAsync(int id) => inner.GetByIdAsync(id);

        public Task<IEnumerable<Product>> GetByIdsAsync(IEnumerable<int> ids) => inner.GetByIdsAsync(ids);

        public Task<IEnumerable<Product>> GetAllAsync() => inner.GetAllAsync();

        public Task<PaginatedResultDto<Product>> SearchAsync<TParameters>(TParameters parameters)
            where TParameters : PaginationParametersBase => inner.SearchAsync(parameters);

        public Task<PaginatedResultDto<Product>> SearchPaginatedAsync(FilterTextPaginatedParameters parameters) =>
            throw new NotSupportedException();

        public void Dispose()
        {
        }
    }

    private sealed class CancelAfterFirstSaveUnitOfWork(UnitOfWork inner, CancellationTokenSource cancellation) : IUnitOfWork
    {
        private int _saveCalls;

        public bool HasActiveTransaction => inner.HasActiveTransaction;

        public async Task CommitChangesAsync()
        {
            await inner.CommitChangesAsync();
            if (Interlocked.Increment(ref _saveCalls) == 1)
                cancellation.Cancel();
        }

        public ITransaction CreateTransaction() => inner.CreateTransaction();

        public void RegisterCommiteable(ICommiteable commiteable) => inner.RegisterCommiteable(commiteable);

        public ITransaction UseCurrentTransaction() => inner.UseCurrentTransaction();

        public void UseTransaction(ITransaction transaction) => inner.UseTransaction(transaction);

        public void UseTransaction(IDbCommand command) => inner.UseTransaction(command);

        public void Dispose() => inner.Dispose();
    }

    #endregion

    #region Public Methods

    [TestMethod]
    public async Task ProvidersPersistSearchVersionDefaultsUniquenessAndAudit()
    {
        string connectionString = GetConnectionString();
        string key = Guid.NewGuid().ToString("N")[..10];
        string correlationId = $"master-data-{key}";
        int userId = await InsertActorAsync(connectionString, key);
        await using ServiceProvider services = CreateServices(connectionString, userId, correlationId);

        try
        {
            using IServiceScope scope = services.CreateScope();
            IProductProvider products = scope.ServiceProvider.GetRequiredService<IProductProvider>();
            ICustomerProvider customers = scope.ServiceProvider.GetRequiredService<ICustomerProvider>();
            IAddressProvider addresses = scope.ServiceProvider.GetRequiredService<IAddressProvider>();
            ICarrierProvider carriers = scope.ServiceProvider.GetRequiredService<ICarrierProvider>();

            ProductDto firstProduct = await products.CreateAsync(new ProductCreateRequest(
                $"sku-{key}", "Alpha Product", "Live Search", 12.3456m, 3, "https://example.test/product.png"), CancellationToken.None);
            ProductDto secondProduct = await products.CreateAsync(new ProductCreateRequest(
                $"other-{key}", "Alpha Product", "Other", 1m, 0, null, false), CancellationToken.None);
            PageResult<ProductDto> productPage = await products.SearchAsync(new ProductSearchRequest
            {
                Search = "alpha product",
                Active = true,
                PageNumber = 1,
                PageSize = 1,
                SortField = "name",
                SortDirection = SortDirection.Asc,
            }, CancellationToken.None);
            Assert.AreEqual(1, productPage.ItemsCount);
            Assert.AreEqual(1, productPage.TotalPages);
            Assert.AreEqual(firstProduct.Id, productPage.Items.Single().Id);
            Assert.IsFalse(string.IsNullOrWhiteSpace(firstProduct.Version));

            PaginatedResultDto<ProductView> officialProductPage = await products.SearchAsync(
                new MasterDataViewSearchParameters
                {
                    Search = "alpha product",
                    Active = true,
                    PageNumber = 1,
                    PageSize = 10,
                    SortBy = "name",
                    SortDirection = "asc",
                });
            ProductView officialProduct = officialProductPage.Results.Single();
            Assert.AreEqual(firstProduct.Id, officialProduct.Id);
            Assert.IsFalse(string.IsNullOrWhiteSpace(officialProduct.CreatedByUserDisplayName));

            MasterDataException duplicateProduct = await Assert.ThrowsAsync<MasterDataException>(() => products.CreateAsync(
                new ProductCreateRequest($"SKU-{key}", "Duplicate", "Other", 1m, 0, null), CancellationToken.None));
            Assert.AreEqual("duplicate_key", duplicateProduct.Code);

            ProductDto deactivated = await products.UpdateAsync(firstProduct.Id, new ProductUpdateRequest(
                firstProduct.Sku, firstProduct.Name, firstProduct.Category, firstProduct.UnitPrice,
                firstProduct.StockQuantity, firstProduct.ThumbnailUrl, false), firstProduct.Version, CancellationToken.None);
            MasterDataException stale = await Assert.ThrowsAsync<MasterDataException>(() => products.UpdateAsync(firstProduct.Id,
                new ProductUpdateRequest(firstProduct.Sku, "Stale overwrite", firstProduct.Category, firstProduct.UnitPrice,
                    firstProduct.StockQuantity, null, true), firstProduct.Version, CancellationToken.None));
            Assert.AreEqual("concurrency_conflict", stale.Code);
            Assert.IsFalse(deactivated.IsActive);

            CustomerDto customer = await customers.CreateAsync(new CustomerCreateRequest(
                $"account-{key}", "Live Customer", "live@example.test", null, 100m, 30), CancellationToken.None);
            MasterDataException duplicateCustomer = await Assert.ThrowsAsync<MasterDataException>(() => customers.CreateAsync(
                new CustomerCreateRequest($"ACCOUNT-{key}", "Duplicate", "duplicate@example.test", null, 0, 0), CancellationToken.None));
            Assert.AreEqual("duplicate_key", duplicateCustomer.Code);
            PaginatedResultDto<CustomerView> officialCustomerPage = await customers.SearchAsync(
                new MasterDataViewSearchParameters { Search = "Live Customer", PageSize = 10, SortBy = "name" });
            Assert.AreEqual(customer.Id, officialCustomerPage.Results.Single().Id);
            Assert.IsFalse(string.IsNullOrWhiteSpace(officialCustomerPage.Results.Single().CreatedByUserDisplayName));

            AddressDto firstAddress = await addresses.CreateAsync(new AddressCreateRequest(
                customer.Id, "shipping", "First", "1 First Street", null, "Buenos Aires", null, "1000", "ar", false, true), CancellationToken.None);
            AddressDto secondAddress = await addresses.CreateAsync(new AddressCreateRequest(
                customer.Id, "both", "Second", "2 Second Street", null, "Buenos Aires", null, "1001", "AR", true, true), CancellationToken.None);
            PageResult<AddressDto> addressPage = await addresses.SearchAsync(new AddressSearchRequest
            {
                CustomerId = customer.Id,
                Usage = AddressUsage.Shipping,
                PageSize = 10,
                SortField = "name",
            }, CancellationToken.None);
            Assert.HasCount(2, addressPage.Items);
            Assert.AreEqual(1, addressPage.Items.Count(address => address.DefaultShipping));
            Assert.AreEqual(secondAddress.Id, addressPage.Items.Single(address => address.DefaultShipping).Id);
            Assert.IsFalse(addressPage.Items.Single(address => address.Id == firstAddress.Id).DefaultShipping);
            Assert.AreEqual("AR", secondAddress.Country);
            PaginatedResultDto<CustomerAddressView> officialAddressPage = await addresses.SearchAsync(
                new MasterDataViewSearchParameters
                {
                    CustomerId = customer.Id,
                    Usage = AddressUsage.Shipping,
                    PageSize = 10,
                    SortBy = "name",
                });
            Assert.AreEqual(2, officialAddressPage.PageInfo.ItemsCount);
            Assert.IsTrue(officialAddressPage.Results.All(address => address.CustomerName == "Live Customer"));

            CarrierDto carrier = await carriers.CreateAsync(new CarrierCreateRequest(
                $"carrier-{key}", "Live Carrier", "Priority", "https://carrier.example/track/{trackingNumber}"), CancellationToken.None);
            PaginatedResultDto<CarrierView> officialCarrierPage = await carriers.SearchAsync(
                new MasterDataViewSearchParameters { Search = "Live Carrier", PageSize = 10, SortBy = "name" });
            Assert.AreEqual(carrier.Id, officialCarrierPage.Results.Single().Id);
            Assert.IsFalse(string.IsNullOrWhiteSpace(officialCarrierPage.Results.Single().CreatedByUserDisplayName));
            MasterDataException duplicateCarrier = await Assert.ThrowsAsync<MasterDataException>(() => carriers.CreateAsync(
                new CarrierCreateRequest($"CARRIER-{key}", "Duplicate", "Priority", null), CancellationToken.None));
            Assert.AreEqual("duplicate_key", duplicateCarrier.Code);
            Assert.IsTrue(carrier.IsActive);

            await AssertAuditAsync(connectionString, correlationId, userId, firstProduct.Id, firstAddress.Id);
            Assert.AreEqual(secondProduct.Id, (await products.GetByIdAsync(secondProduct.Id, CancellationToken.None)).Id);

            MasterDataException staleDelete = await Assert.ThrowsAsync<MasterDataException>(() =>
                products.DeleteAsync(firstProduct.Id, firstProduct.Version, CancellationToken.None));
            Assert.AreEqual("concurrency_conflict", staleDelete.Code);
            await products.DeleteAsync(secondProduct.Id, secondProduct.Version, CancellationToken.None);
            await carriers.DeleteAsync(carrier.Id, carrier.Version, CancellationToken.None);

            MasterDataException referencedCustomer = await Assert.ThrowsAsync<MasterDataException>(() =>
                customers.DeleteAsync(customer.Id, customer.Version, CancellationToken.None));
            Assert.AreEqual("referenced_record", referencedCustomer.Code);
            AddressDto currentFirstAddress = await addresses.GetByIdAsync(firstAddress.Id, CancellationToken.None);
            await addresses.DeleteAsync(currentFirstAddress.Id, currentFirstAddress.Version, CancellationToken.None);
            await addresses.DeleteAsync(secondAddress.Id, secondAddress.Version, CancellationToken.None);
            await customers.DeleteAsync(customer.Id, customer.Version, CancellationToken.None);
        }
        finally
        {
            await CleanupAsync(connectionString, key, correlationId, userId);
        }
    }

    [TestMethod]
    public async Task ConcurrentUniqueConflictClearsTrackerAndAllowsSameScopeRecovery()
    {
        string connectionString = GetConnectionString();
        string key = Guid.NewGuid().ToString("N")[..10];
        string correlationId = $"master-data-race-{key}";
        int userId = await InsertActorAsync(connectionString, key);
        var gate = new SkuRaceGate();
        await using ServiceProvider services = CreateServices(connectionString, userId, correlationId, gate);
        using IServiceScope firstScope = services.CreateScope();
        using IServiceScope secondScope = services.CreateScope();
        IProductProvider first = firstScope.ServiceProvider.GetRequiredService<IProductProvider>();
        IProductProvider second = secondScope.ServiceProvider.GetRequiredService<IProductProvider>();

        try
        {
            ProductCreateRequest request = new($"race-{key}", "Concurrent product", "Race", 1m, 0, null);
            Task<(ProductDto? Result, Exception? Error)> firstAttempt = CaptureCreateAsync(first, request);
            Task<(ProductDto? Result, Exception? Error)> secondAttempt = CaptureCreateAsync(second, request);
            (ProductDto? Result, Exception? Error)[] attempts = await Task.WhenAll(firstAttempt, secondAttempt);

            Assert.AreEqual(1, attempts.Count(attempt => attempt.Result is not null));
            MasterDataException conflict = (MasterDataException)attempts.Single(attempt => attempt.Error is not null).Error!;
            Assert.AreEqual("duplicate_key", conflict.Code);

            IProductProvider failedProvider = attempts[0].Error is not null ? first : second;
            ProductDto recovered = await failedProvider.CreateAsync(new ProductCreateRequest(
                $"recovered-{key}", "Recovered product", "Race", 2m, 0, null), CancellationToken.None);
            Assert.IsTrue(recovered.Id > 0);
            Assert.AreEqual(2, await CountProductsAsync(connectionString, key));
        }
        finally
        {
            await CleanupAsync(connectionString, key, correlationId, userId);
        }
    }

    [TestMethod]
    public async Task InverseAddressMovesAcquireCustomerLocksWithoutDeadlock()
    {
        string connectionString = GetConnectionString();
        string key = Guid.NewGuid().ToString("N")[..10];
        string correlationId = $"master-data-lock-{key}";
        int userId = await InsertActorAsync(connectionString, key);
        await using ServiceProvider services = CreateServices(connectionString, userId, correlationId);

        try
        {
            CustomerDto firstCustomer;
            CustomerDto secondCustomer;
            AddressDto firstAddress;
            AddressDto secondAddress;
            using (IServiceScope setupScope = services.CreateScope())
            {
                ICustomerProvider customers = setupScope.ServiceProvider.GetRequiredService<ICustomerProvider>();
                IAddressProvider addresses = setupScope.ServiceProvider.GetRequiredService<IAddressProvider>();
                firstCustomer = await customers.CreateAsync(new CustomerCreateRequest(
                    $"lock-a-{key}", "Lock A", "lock-a@example.test", null, 0, 0), CancellationToken.None);
                secondCustomer = await customers.CreateAsync(new CustomerCreateRequest(
                    $"lock-b-{key}", "Lock B", "lock-b@example.test", null, 0, 0), CancellationToken.None);
                firstAddress = await addresses.CreateAsync(AddressRequest(firstCustomer.Id, "First"), CancellationToken.None);
                secondAddress = await addresses.CreateAsync(AddressRequest(secondCustomer.Id, "Second"), CancellationToken.None);
            }

            for (int iteration = 0; iteration < 5; iteration++)
            {
                using IServiceScope firstMoveScope = services.CreateScope();
                using IServiceScope secondMoveScope = services.CreateScope();
                IAddressProvider firstMover = firstMoveScope.ServiceProvider.GetRequiredService<IAddressProvider>();
                IAddressProvider secondMover = secondMoveScope.ServiceProvider.GetRequiredService<IAddressProvider>();
                int firstDestination = firstAddress.CustomerId == firstCustomer.Id ? secondCustomer.Id : firstCustomer.Id;
                int secondDestination = secondAddress.CustomerId == secondCustomer.Id ? firstCustomer.Id : secondCustomer.Id;

                Task<AddressDto> firstMove = firstMover.UpdateAsync(firstAddress.Id,
                    AddressUpdate(firstDestination, firstAddress.Label), firstAddress.Version, CancellationToken.None);
                Task<AddressDto> secondMove = secondMover.UpdateAsync(secondAddress.Id,
                    AddressUpdate(secondDestination, secondAddress.Label), secondAddress.Version, CancellationToken.None);
                AddressDto[] moved = await Task.WhenAll(firstMove, secondMove).WaitAsync(TimeSpan.FromSeconds(15));
                firstAddress = moved[0];
                secondAddress = moved[1];
            }

            Assert.AreNotEqual(firstAddress.CustomerId, secondAddress.CustomerId);
        }
        finally
        {
            await CleanupAsync(connectionString, key, correlationId, userId);
        }
    }

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    public async Task DefaultAddressReparentClearsDestinationDefaults(bool billing, bool shipping)
    {
        string connectionString = GetConnectionString();
        string key = Guid.NewGuid().ToString("N")[..10];
        string correlationId = $"master-data-default-move-{key}";
        int userId = await InsertActorAsync(connectionString, key);
        await using ServiceProvider services = CreateServices(connectionString, userId, correlationId);

        try
        {
            using IServiceScope scope = services.CreateScope();
            ICustomerProvider customers = scope.ServiceProvider.GetRequiredService<ICustomerProvider>();
            IAddressProvider addresses = scope.ServiceProvider.GetRequiredService<IAddressProvider>();
            CustomerDto source = await customers.CreateAsync(new CustomerCreateRequest(
                $"move-source-{key}", "Move Source", "source@example.test", null, 0, 0), CancellationToken.None);
            CustomerDto destination = await customers.CreateAsync(new CustomerCreateRequest(
                $"move-destination-{key}", "Move Destination", "destination@example.test", null, 0, 0), CancellationToken.None);
            AddressDto moving = await addresses.CreateAsync(AddressRequest(source.Id, "Moving") with
            {
                DefaultBilling = billing,
                DefaultShipping = shipping,
            }, CancellationToken.None);
            AddressDto previous = await addresses.CreateAsync(AddressRequest(destination.Id, "Previous") with
            {
                DefaultBilling = billing,
                DefaultShipping = shipping,
            }, CancellationToken.None);

            AddressDto moved = await addresses.UpdateAsync(moving.Id, AddressUpdate(destination.Id, moving.Label) with
            {
                DefaultBilling = billing,
                DefaultShipping = shipping,
            }, moving.Version, CancellationToken.None);
            AddressDto cleared = await addresses.GetByIdAsync(previous.Id, CancellationToken.None);

            Assert.AreEqual(destination.Id, moved.CustomerId);
            Assert.AreEqual(billing, moved.DefaultBilling);
            Assert.AreEqual(shipping, moved.DefaultShipping);
            Assert.IsFalse(cleared.DefaultBilling);
            Assert.IsFalse(cleared.DefaultShipping);
        }
        finally
        {
            await CleanupAsync(connectionString, key, correlationId, userId);
        }
    }

    [TestMethod]
    public async Task SearchRoutinesTreatMetacharactersLiterallyAndHandlePageEdges()
    {
        string connectionString = GetConnectionString();
        string key = Guid.NewGuid().ToString("N")[..10];
        string correlationId = $"master-data-search-{key}";
        int userId = await InsertActorAsync(connectionString, key);
        await using ServiceProvider services = CreateServices(connectionString, userId, correlationId);

        try
        {
            using IServiceScope scope = services.CreateScope();
            IProductProvider products = scope.ServiceProvider.GetRequiredService<IProductProvider>();
            ICustomerProvider customers = scope.ServiceProvider.GetRequiredService<ICustomerProvider>();
            IAddressProvider addresses = scope.ServiceProvider.GetRequiredService<IAddressProvider>();
            ICarrierProvider carriers = scope.ServiceProvider.GetRequiredService<ICarrierProvider>();
            string literal = $"%_[{key}";
            ProductDto product = await products.CreateAsync(new ProductCreateRequest(
                $"p-{key}", $"Product {literal}", "Search", 1m, 0, null), CancellationToken.None);
            CustomerDto customer = await customers.CreateAsync(new CustomerCreateRequest(
                $"c-{key}", $"Customer {literal}", $"{key}@example.test", null, 0, 0), CancellationToken.None);
            AddressDto address = await addresses.CreateAsync(AddressRequest(customer.Id, $"Address {literal}"), CancellationToken.None);
            CarrierDto carrier = await carriers.CreateAsync(new CarrierCreateRequest(
                $"k-{key}", $"Carrier {literal}", "Search", null), CancellationToken.None);

            Assert.AreEqual(product.Id, (await products.SearchAsync(new ProductSearchRequest { Search = literal, Active = true }, CancellationToken.None)).Items.Single().Id);
            Assert.AreEqual(customer.Id, (await customers.SearchAsync(new CustomerSearchRequest { Search = literal, Active = true }, CancellationToken.None)).Items.Single().Id);
            Assert.AreEqual(address.Id, (await addresses.SearchAsync(new AddressSearchRequest { Search = literal, CustomerId = customer.Id }, CancellationToken.None)).Items.Single().Id);
            Assert.AreEqual(carrier.Id, (await carriers.SearchAsync(new CarrierSearchRequest { Search = literal, Active = true }, CancellationToken.None)).Items.Single().Id);

            Assert.HasCount(0, (await products.SearchAsync(new ProductSearchRequest { PageNumber = int.MaxValue, PageSize = 100 }, CancellationToken.None)).Items);
            Assert.HasCount(0, (await customers.SearchAsync(new CustomerSearchRequest { PageNumber = int.MaxValue, PageSize = 100 }, CancellationToken.None)).Items);
            Assert.HasCount(0, (await addresses.SearchAsync(new AddressSearchRequest { PageNumber = int.MaxValue, PageSize = 100 }, CancellationToken.None)).Items);
            Assert.HasCount(0, (await carriers.SearchAsync(new CarrierSearchRequest { PageNumber = int.MaxValue, PageSize = 100 }, CancellationToken.None)).Items);

            string maximumSearch = new('x', 320);
            Assert.HasCount(0, (await products.SearchAsync(new ProductSearchRequest { Search = maximumSearch }, CancellationToken.None)).Items);
            Assert.HasCount(0, (await customers.SearchAsync(new CustomerSearchRequest { Search = maximumSearch }, CancellationToken.None)).Items);
            Assert.HasCount(0, (await addresses.SearchAsync(new AddressSearchRequest { Search = maximumSearch }, CancellationToken.None)).Items);
            Assert.HasCount(0, (await carriers.SearchAsync(new CarrierSearchRequest { Search = maximumSearch }, CancellationToken.None)).Items);

            string tooLong = new('x', 321);
            await Assert.ThrowsAsync<MasterDataValidationException>(() => products.SearchAsync(new ProductSearchRequest { Search = tooLong }, CancellationToken.None));
            await Assert.ThrowsAsync<MasterDataValidationException>(() => customers.SearchAsync(new CustomerSearchRequest { Search = tooLong }, CancellationToken.None));
            await Assert.ThrowsAsync<MasterDataValidationException>(() => addresses.SearchAsync(new AddressSearchRequest { Search = tooLong }, CancellationToken.None));
            await Assert.ThrowsAsync<MasterDataValidationException>(() => carriers.SearchAsync(new CarrierSearchRequest { Search = tooLong }, CancellationToken.None));
        }
        finally
        {
            await CleanupAsync(connectionString, key, correlationId, userId);
        }
    }

    [TestMethod]
    public async Task CancellationAfterResourceSaveRollsBackResourceAndAudit()
    {
        string connectionString = GetConnectionString();
        string key = Guid.NewGuid().ToString("N")[..10];
        string correlationId = $"master-data-rollback-{key}";
        int userId = await InsertActorAsync(connectionString, key);
        using var cancellation = new CancellationTokenSource();
        await using ServiceProvider services = CreateServices(connectionString, userId, correlationId,
            cancellationAfterFirstSave: cancellation);

        try
        {
            using IServiceScope scope = services.CreateScope();
            IProductProvider products = scope.ServiceProvider.GetRequiredService<IProductProvider>();
            await Assert.ThrowsAsync<OperationCanceledException>(() => products.CreateAsync(new ProductCreateRequest(
                $"rollback-{key}", "Rollback product", "Rollback", 1m, 0, null), cancellation.Token));

            Assert.AreEqual(0, await CountProductsAsync(connectionString, key));
            Assert.AreEqual(0, await CountAuditsAsync(connectionString, correlationId));
        }
        finally
        {
            await CleanupAsync(connectionString, key, correlationId, userId);
        }
    }

    [TestMethod]
    public async Task ConcurrentDefaultReplacementLeavesOneBillingAndShippingDefault()
    {
        string connectionString = GetConnectionString();
        string key = Guid.NewGuid().ToString("N")[..10];
        string correlationId = $"master-data-default-race-{key}";
        int userId = await InsertActorAsync(connectionString, key);
        await using ServiceProvider services = CreateServices(connectionString, userId, correlationId);

        try
        {
            CustomerDto customer;
            AddressDto first;
            AddressDto second;
            using (IServiceScope setup = services.CreateScope())
            {
                ICustomerProvider customers = setup.ServiceProvider.GetRequiredService<ICustomerProvider>();
                IAddressProvider addresses = setup.ServiceProvider.GetRequiredService<IAddressProvider>();
                customer = await customers.CreateAsync(new CustomerCreateRequest(
                    $"default-race-{key}", "Default Race", "race@example.test", null, 0, 0), CancellationToken.None);
                first = await addresses.CreateAsync(AddressRequest(customer.Id, "First"), CancellationToken.None);
                second = await addresses.CreateAsync(AddressRequest(customer.Id, "Second"), CancellationToken.None);
            }

            using IServiceScope firstScope = services.CreateScope();
            using IServiceScope secondScope = services.CreateScope();
            IAddressProvider firstProvider = firstScope.ServiceProvider.GetRequiredService<IAddressProvider>();
            IAddressProvider secondProvider = secondScope.ServiceProvider.GetRequiredService<IAddressProvider>();
            await Task.WhenAll(
                firstProvider.UpdateAsync(first.Id, AddressUpdate(customer.Id, first.Label) with
                {
                    DefaultBilling = true,
                    DefaultShipping = true,
                }, first.Version, CancellationToken.None),
                secondProvider.UpdateAsync(second.Id, AddressUpdate(customer.Id, second.Label) with
                {
                    DefaultBilling = true,
                    DefaultShipping = true,
                }, second.Version, CancellationToken.None)).WaitAsync(TimeSpan.FromSeconds(15));

            using IServiceScope readScope = services.CreateScope();
            PageResult<AddressDto> page = await readScope.ServiceProvider.GetRequiredService<IAddressProvider>()
                .SearchAsync(new AddressSearchRequest { CustomerId = customer.Id, PageSize = 10 }, CancellationToken.None);
            Assert.AreEqual(1, page.Items.Count(item => item.DefaultBilling));
            Assert.AreEqual(1, page.Items.Count(item => item.DefaultShipping));
        }
        finally
        {
            await CleanupAsync(connectionString, key, correlationId, userId);
        }
    }

    [TestMethod]
    public async Task ProvidersProtectEveryMasterDataReferenceKind()
    {
        string connectionString = GetConnectionString();
        string key = Guid.NewGuid().ToString("N")[..10];
        string correlationId = $"master-data-references-{key}";
        int userId = await InsertActorAsync(connectionString, key);
        await using ServiceProvider services = CreateServices(connectionString, userId, correlationId);

        try
        {
            using IServiceScope scope = services.CreateScope();
            IProductProvider products = scope.ServiceProvider.GetRequiredService<IProductProvider>();
            ICustomerProvider customers = scope.ServiceProvider.GetRequiredService<ICustomerProvider>();
            IAddressProvider addresses = scope.ServiceProvider.GetRequiredService<IAddressProvider>();
            ICarrierProvider carriers = scope.ServiceProvider.GetRequiredService<ICarrierProvider>();
            ProductDto product = await products.CreateAsync(new ProductCreateRequest(
                $"ref-{key}", "Referenced product", "References", 1m, 0, null), CancellationToken.None);
            CustomerDto customer = await customers.CreateAsync(new CustomerCreateRequest(
                $"ref-{key}", "Referenced customer", "referenced@example.test", null, 0, 0), CancellationToken.None);
            AddressDto address = await addresses.CreateAsync(AddressRequest(customer.Id, "Referenced"), CancellationToken.None);
            CarrierDto carrier = await carriers.CreateAsync(new CarrierCreateRequest(
                $"ref-{key}", "Referenced carrier", "Ground", null), CancellationToken.None);
            await InsertReferencesAsync(connectionString, key, userId, product.Id, customer.Id, address.Id, carrier.Id);

            Assert.AreEqual("referenced_record", (await Assert.ThrowsAsync<MasterDataException>(() =>
                products.DeleteAsync(product.Id, product.Version, CancellationToken.None))).Code);
            Assert.AreEqual("referenced_record", (await Assert.ThrowsAsync<MasterDataException>(() =>
                customers.DeleteAsync(customer.Id, customer.Version, CancellationToken.None))).Code);
            Assert.AreEqual("referenced_address", (await Assert.ThrowsAsync<MasterDataException>(() =>
                addresses.DeleteAsync(address.Id, address.Version, CancellationToken.None))).Code);
            Assert.AreEqual("referenced_record", (await Assert.ThrowsAsync<MasterDataException>(() =>
                carriers.DeleteAsync(carrier.Id, carrier.Version, CancellationToken.None))).Code);
        }
        finally
        {
            await CleanupAsync(connectionString, key, correlationId, userId);
        }
    }

    #endregion

    #region Private Methods

    private static ServiceProvider CreateServices(string connectionString, int userId, string correlationId,
        SkuRaceGate? skuRaceGate = null, CancellationTokenSource? cancellationAfterFirstSave = null)
    {
        var services = new ServiceCollection();
        services.RegisterLoggedUserService();
        if (cancellationAfterFirstSave is null)
            services.AddScoped<IUnitOfWork, UnitOfWork>();
        else
        {
            services.AddScoped<UnitOfWork>();
            services.AddScoped<IUnitOfWork>(provider => new CancelAfterFirstSaveUnitOfWork(
                provider.GetRequiredService<UnitOfWork>(), cancellationAfterFirstSave));
        }
        services.AddDbContext<ReceivablesDbContext>((_, options) => options.UseSqlServer(connectionString));
        services.AddScoped<ProductRepository>();
        if (skuRaceGate is null)
            services.AddScoped<IProductRepository>(provider => provider.GetRequiredService<ProductRepository>());
        else
        {
            services.AddSingleton(skuRaceGate);
            services.AddScoped<IProductRepository>(provider => new CoordinatedProductRepository(
                provider.GetRequiredService<ProductRepository>(), provider.GetRequiredService<SkuRaceGate>()));
        }
        services.AddScoped<IProductViewRepository, ProductViewRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICustomerViewRepository, CustomerViewRepository>();
        services.AddScoped<IAddressRepository, AddressRepository>();
        services.AddScoped<IAddressViewRepository, AddressViewRepository>();
        services.AddScoped<ICarrierRepository, CarrierRepository>();
        services.AddScoped<ICarrierViewRepository, CarrierViewRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IMasterDataPersistenceErrorClassifier, MasterDataPersistenceErrorClassifier>();
        services.AddScoped<IPersistenceSession, PersistenceSession>();
        services.AddScoped<IApplicationOperationContext>(_ => new TestOperationContext(userId, correlationId));
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(new DateTimeOffset(2026, 8, 1, 15, 30, 0, TimeSpan.Zero)));
        services.AddScoped<IProductProvider, ProductProvider>();
        services.AddScoped<ICustomerProvider, CustomerProvider>();
        services.AddScoped<IAddressProvider, AddressProvider>();
        services.AddScoped<ICarrierProvider, CarrierProvider>();
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static async Task<(ProductDto? Result, Exception? Error)> CaptureCreateAsync(
        IProductProvider provider, ProductCreateRequest request)
    {
        try
        {
            return (await provider.CreateAsync(request, CancellationToken.None), null);
        }
        catch (Exception exception)
        {
            return (null, exception);
        }
    }

    private static AddressCreateRequest AddressRequest(int customerId, string label) => new(
        customerId, "both", label, $"{label} Street", null, "Buenos Aires", null, "1000", "AR", false, false);

    private static AddressUpdateRequest AddressUpdate(int customerId, string label) => new(
        customerId, "both", label, $"{label} Street", null, "Buenos Aires", null, "1000", "AR", false, false);

    private static async Task<int> CountProductsAsync(string connectionString, string key)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            "SELECT COUNT(1) FROM [dbo].[Product] WHERE [Sku] LIKE @Suffix", connection);
        command.Parameters.AddWithValue("@Suffix", $"%{key}");
        return Convert.ToInt32(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<int> CountAuditsAsync(string connectionString, string correlationId)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            "SELECT COUNT(1) FROM [dbo].[AuditLog] WHERE [CorrelationId] = @CorrelationId", connection);
        command.Parameters.AddWithValue("@CorrelationId", correlationId);
        return Convert.ToInt32(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task InsertReferencesAsync(string connectionString, string key, int userId,
        int productId, int customerId, int addressId, int carrierId)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        const string sql = """
            DECLARE @QuoteId INT;
            INSERT INTO [dbo].[Quote]
                ([QuoteNumber], [CustomerId], [ShippingAddressId], [QuoteDate], [ValidUntil], [StatusId],
                 [CustomerAccountNumberSnapshot], [CustomerNameSnapshot], [CustomerEmailSnapshot],
                 [ShippingLabelSnapshot], [ShippingLine1Snapshot], [ShippingCitySnapshot],
                 [ShippingPostalCodeSnapshot], [ShippingCountrySnapshot], [ShippingAddressTypeCodeSnapshot],
                 [CreatedByUserId], [CreationDate])
            VALUES
                (@QuoteNumber, @CustomerId, @AddressId, '2026-08-01', '2026-08-31', 1,
                 N'ACCOUNT', N'Customer', N'referenced@example.test', N'Address', N'Line 1', N'City',
                 N'1000', N'AR', N'both', @UserId, SYSDATETIMEOFFSET());
            SET @QuoteId = SCOPE_IDENTITY();
            INSERT INTO [dbo].[QuoteLine]
                ([QuoteId], [ProductId], [SkuSnapshot], [ProductNameSnapshot], [Quantity], [UnitPrice], [DiscountPercent])
            VALUES (@QuoteId, @ProductId, N'SKU', N'Product', 1, 1, 0);

            DECLARE @OrderId INT;
            INSERT INTO [dbo].[SalesOrder]
                ([OrderNumber], [CustomerId], [ShippingAddressId], [StatusId], [CarrierId], [CreatedByUserId], [CreationDate])
            VALUES (@OrderNumber, @CustomerId, @AddressId, 1, @CarrierId, @UserId, SYSDATETIMEOFFSET());
            SET @OrderId = SCOPE_IDENTITY();
            INSERT INTO [dbo].[SalesOrderLine]
                ([SalesOrderId], [ProductId], [SkuSnapshot], [ProductNameSnapshot], [Quantity], [UnitPrice], [DiscountPercent])
            VALUES (@OrderId, @ProductId, N'SKU', N'Product', 1, 1, 0);
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@QuoteNumber", $"Q-{key}");
        command.Parameters.AddWithValue("@OrderNumber", $"O-{key}");
        command.Parameters.AddWithValue("@CustomerId", customerId);
        command.Parameters.AddWithValue("@AddressId", addressId);
        command.Parameters.AddWithValue("@ProductId", productId);
        command.Parameters.AddWithValue("@CarrierId", carrierId);
        command.Parameters.AddWithValue("@UserId", userId);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<int> InsertActorAsync(string connectionString, string key)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        const string sql = "INSERT INTO [dbo].[ApplicationUser] ([Issuer], [Subject], [DisplayName], [CreationDate]) OUTPUT INSERTED.[Id] VALUES (N'https://tests.beacon.local', @Subject, N'Master data test', SYSDATETIMEOFFSET());";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Subject", key);
        return Convert.ToInt32(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task AssertAuditAsync(string connectionString, string correlationId, int userId, int productId, int firstAddressId)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        const string sql = "SELECT [ResourceType], [ResourceId], [Action], [UserId], [MetadataJson] FROM [dbo].[AuditLog] WHERE [CorrelationId] = @CorrelationId ORDER BY [Id];";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CorrelationId", correlationId);
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        var rows = new List<(string Resource, string Id, string Action, int UserId, string? Metadata)>();
        while (await reader.ReadAsync())
            rows.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3), reader.IsDBNull(4) ? null : reader.GetString(4)));

        Assert.IsTrue(rows.Count >= 8);
        Assert.IsTrue(rows.All(row => row.UserId == userId));
        Assert.IsTrue(rows.Any(row => row.Resource == "product" && row.Id == productId.ToString(System.Globalization.CultureInfo.InvariantCulture) && row.Action == "created"));
        Assert.IsTrue(rows.Any(row => row.Resource == "address" && row.Id == firstAddressId.ToString(System.Globalization.CultureInfo.InvariantCulture) && row.Action == "defaultShippingCleared"));
        Assert.IsFalse(rows.Any(row => row.Metadata?.Contains("example.test", StringComparison.OrdinalIgnoreCase) == true));
    }

    private static async Task CleanupAsync(string connectionString, string key, string correlationId, int userId)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        const string sql = """
            DELETE FROM [dbo].[AuditLog] WHERE [CorrelationId] = @CorrelationId;
            DELETE FROM [dbo].[SalesOrderLine] WHERE [SalesOrderId] IN (SELECT [Id] FROM [dbo].[SalesOrder] WHERE [CustomerId] IN (SELECT [Id] FROM [dbo].[Customer] WHERE [AccountNumber] LIKE @AccountPrefix));
            DELETE FROM [dbo].[SalesOrder] WHERE [CustomerId] IN (SELECT [Id] FROM [dbo].[Customer] WHERE [AccountNumber] LIKE @AccountPrefix);
            DELETE FROM [dbo].[QuoteLine] WHERE [QuoteId] IN (SELECT [Id] FROM [dbo].[Quote] WHERE [CustomerId] IN (SELECT [Id] FROM [dbo].[Customer] WHERE [AccountNumber] LIKE @AccountPrefix));
            DELETE FROM [dbo].[Quote] WHERE [CustomerId] IN (SELECT [Id] FROM [dbo].[Customer] WHERE [AccountNumber] LIKE @AccountPrefix);
            DELETE FROM [dbo].[CustomerAddress] WHERE [CustomerId] IN (SELECT [Id] FROM [dbo].[Customer] WHERE [AccountNumber] LIKE @AccountPrefix);
            DELETE FROM [dbo].[Product] WHERE [Sku] LIKE @SkuSuffix;
            DELETE FROM [dbo].[Carrier] WHERE [Code] LIKE @CarrierSuffix;
            DELETE FROM [dbo].[Customer] WHERE [AccountNumber] LIKE @AccountPrefix;
            DELETE FROM [dbo].[ApplicationUser] WHERE [Id] = @UserId;
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CorrelationId", correlationId);
        command.Parameters.AddWithValue("@AccountPrefix", $"%{key}");
        command.Parameters.AddWithValue("@SkuSuffix", $"%{key}");
        command.Parameters.AddWithValue("@CarrierSuffix", $"%{key}");
        command.Parameters.AddWithValue("@UserId", userId);
        await command.ExecuteNonQueryAsync();
    }

    private static string GetConnectionString()
    {
        string? connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DatabaseConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            Assert.Inconclusive("Set ConnectionStrings__DatabaseConnection to run database integration tests.");
        return connectionString;
    }

    #endregion
}
