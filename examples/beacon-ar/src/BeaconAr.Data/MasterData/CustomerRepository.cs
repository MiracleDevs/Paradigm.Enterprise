using BeaconAr.Data.Receivables;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.Receivables.Generated;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Repositories;

namespace BeaconAr.Data.MasterData;

public sealed class CustomerRepository : RepositoryBase<ReceivablesDbContext, int>, ICustomerRepository
{
    #region Constructors

    public CustomerRepository(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    #endregion

    #region Public Methods

    public Task<Customer?> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        EntityContext.Customers.SingleOrDefaultAsync(customer => customer.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken) =>
        EntityContext.Customers.AsNoTracking().AnyAsync(customer => customer.Id == id, cancellationToken);

    public Task<bool> AccountNumberExistsAsync(string accountNumber, int? excludedId, CancellationToken cancellationToken) =>
        EntityContext.Customers.AsNoTracking().AnyAsync(
            customer => customer.AccountNumber == accountNumber && (!excludedId.HasValue || customer.Id != excludedId.Value), cancellationToken);

    public async Task<bool> HasReferencesAsync(int id, CancellationToken cancellationToken) =>
        await EntityContext.CustomerAddresses.AsNoTracking().AnyAsync(address => address.CustomerId == id, cancellationToken) ||
        await EntityContext.Quotes.AsNoTracking().AnyAsync(quote => quote.CustomerId == id, cancellationToken) ||
        await EntityContext.SalesOrders.AsNoTracking().AnyAsync(order => order.CustomerId == id, cancellationToken);

    public void Add(Customer customer) => EntityContext.Customers.Add(customer);

    public void Delete(Customer customer) => EntityContext.Customers.Remove(customer);

    #endregion
}
