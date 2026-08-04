using BeaconAr.Data.Sales.Context;
using BeaconAr.Data.Sales.StoredProcedures;
using BeaconAr.Domain.Sales.Repositories;
using Paradigm.Enterprise.Data.Repositories;

namespace BeaconAr.Data.Sales.Repositories;

public sealed class SalesReferenceRepository : RepositoryBase<SalesDbContext, int>, ISalesReferenceRepository
{
    #region Fields

    private static readonly GetAddressSalesReferenceForUpdateProcedure AddressProcedure = new();
    private static readonly GetCarrierSalesReferenceForUpdateProcedure CarrierProcedure = new();
    private static readonly GetCustomerSalesReferenceForUpdateProcedure CustomerProcedure = new();
    private static readonly GetProductSalesReferenceForUpdateProcedure ProductProcedure = new();

    #endregion

    #region Constructors

    public SalesReferenceRepository(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    #endregion

    #region Public Methods

    public async Task<CustomerSalesReference?> GetCustomerAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CustomerSalesReferenceRow? row = await CustomerProcedure.ExecuteAsync(GetDbConnection(),
            new GetCustomerSalesReferenceForUpdateParameters { Id = id }, UnitOfWork);
        cancellationToken.ThrowIfCancellationRequested();
        return row is null ? null : new(row.Id, row.AccountNumber, row.Name, row.Email, row.Phone, row.IsActive);
    }

    public async Task<AddressSalesReference?> GetAddressAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AddressSalesReferenceRow? row = await AddressProcedure.ExecuteAsync(GetDbConnection(),
            new GetAddressSalesReferenceForUpdateParameters { Id = id }, UnitOfWork);
        cancellationToken.ThrowIfCancellationRequested();
        return row is null ? null : new(row.Id, row.CustomerId, row.AddressTypeCode, row.Label, row.Line1,
            row.Line2, row.City, row.State, row.PostalCode, row.Country);
    }

    public async Task<IReadOnlyDictionary<int, ProductSalesReference>> GetProductsAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken)
    {
        Dictionary<int, ProductSalesReference> products = [];
        foreach (int id in ids.Distinct().OrderBy(id => id))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ProductSalesReferenceRow? row = await ProductProcedure.ExecuteAsync(GetDbConnection(),
                new GetProductSalesReferenceForUpdateParameters { Id = id }, UnitOfWork);
            if (row is not null)
                products.Add(row.Id, new ProductSalesReference(row.Id, row.Sku, row.Name, row.IsActive));
        }
        return products;
    }

    public async Task<CarrierSalesReference?> GetCarrierAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CarrierSalesReferenceRow? row = await CarrierProcedure.ExecuteAsync(GetDbConnection(),
            new GetCarrierSalesReferenceForUpdateParameters { Id = id }, UnitOfWork);
        cancellationToken.ThrowIfCancellationRequested();
        return row is null ? null : new(row.Id, row.Name, row.IsActive);
    }

    #endregion
}
