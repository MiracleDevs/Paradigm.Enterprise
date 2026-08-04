using BeaconAr.Data.MasterData.Context;
using BeaconAr.Data.MasterData.StoredProcedures;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.MasterData.Entities;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Repositories;

namespace BeaconAr.Data.MasterData.Repositories;

public sealed class CustomerRepository : EditRepositoryBase<Customer, MasterDataDbContext, int>, ICustomerRepository
{
    #region Fields

    private static readonly HasCustomerReferencesProcedure HasReferencesProcedure = new();

    #endregion

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

    public async Task<bool> HasReferencesAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        bool result = await HasReferencesProcedure.ExecuteAsync(GetDbConnection(), new HasCustomerReferencesParameters { Id = id }, UnitOfWork);
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }

    #endregion
}
