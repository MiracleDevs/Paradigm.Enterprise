using BeaconAr.Data.Receivables.Context;
using BeaconAr.Domain.Sales.Repositories;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Repositories;

namespace BeaconAr.Data.Sales;

public sealed class SalesReferenceRepository : RepositoryBase<ReceivablesDbContext, int>, ISalesReferenceRepository
{
    #region Constructors

    public SalesReferenceRepository(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    #endregion

    #region Public Methods

    public Task<CustomerSalesReference?> GetCustomerAsync(int id, CancellationToken cancellationToken) =>
        EntityContext.Customers
            .FromSqlInterpolated($"SELECT * FROM [dbo].[Customer] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id}")
            .AsNoTracking()
            .Select(customer => new CustomerSalesReference(customer.Id, customer.AccountNumber, customer.Name,
                customer.Email, customer.Phone, customer.IsActive))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<AddressSalesReference?> GetAddressAsync(int id, CancellationToken cancellationToken) =>
        EntityContext.CustomerAddresses
            .FromSqlInterpolated($"SELECT * FROM [dbo].[CustomerAddress] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id}")
            .AsNoTracking()
            .Select(address => new AddressSalesReference(address.Id, address.CustomerId, address.AddressType.Code,
                address.Label, address.Line1, address.Line2, address.City, address.State, address.PostalCode, address.Country))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<int, ProductSalesReference>> GetProductsAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken)
    {
        Dictionary<int, ProductSalesReference> products = [];
        foreach (int id in ids.Distinct().OrderBy(id => id))
        {
            ProductSalesReference? product = await EntityContext.Products
                .FromSqlInterpolated($"SELECT * FROM [dbo].[Product] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id}")
                .AsNoTracking()
                .Select(product => new ProductSalesReference(product.Id, product.Sku, product.Name, product.IsActive))
                .SingleOrDefaultAsync(cancellationToken);
            if (product is not null)
                products.Add(product.Id, product);
        }
        return products;
    }

    public Task<CarrierSalesReference?> GetCarrierAsync(int id, CancellationToken cancellationToken) =>
        EntityContext.Carriers
            .FromSqlInterpolated($"SELECT * FROM [dbo].[Carrier] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id}")
            .AsNoTracking()
            .Select(carrier => new CarrierSalesReference(carrier.Id, carrier.Name, carrier.IsActive))
            .SingleOrDefaultAsync(cancellationToken);

    #endregion
}
