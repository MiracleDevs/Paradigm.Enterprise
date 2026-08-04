using BeaconAr.Data.MasterData.Context;
using BeaconAr.Data.MasterData.StoredProcedures;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.MasterData.Entities;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Repositories;

namespace BeaconAr.Data.MasterData.Repositories;

public sealed class AddressRepository : EditRepositoryBase<CustomerAddress, MasterDataDbContext, int>, IAddressRepository
{
    #region Fields

    private static readonly HasAddressReferencesProcedure HasReferencesProcedure = new();
    private static readonly LockCustomerForAddressMutationProcedure LockCustomerProcedure = new();

    #endregion

    #region Constructors

    public AddressRepository(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    #endregion

    #region Public Methods

    public Task<CustomerAddress?> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        EntityContext.CustomerAddresses.SingleOrDefaultAsync(address => address.Id == id, cancellationToken);

    public Task<int?> GetActiveTypeIdAsync(string code, CancellationToken cancellationToken) =>
        EntityContext.AddressTypes.AsNoTracking()
            .Where(type => type.Code == code && type.IsActive)
            .Select(type => (int?)type.Id)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<bool> HasReferencesAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        bool result = await HasReferencesProcedure.ExecuteAsync(GetDbConnection(), new HasAddressReferencesParameters { Id = id }, UnitOfWork);
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }

    public async Task LockCustomersAsync(IEnumerable<int> customerIds, CancellationToken cancellationToken)
    {
        int[] ids = customerIds.Distinct().OrderBy(id => id).ToArray();
        if (ids.Length == 0)
            return;

        foreach (int id in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await LockCustomerProcedure.ExecuteAsync(GetDbConnection(), new LockCustomerForAddressMutationParameters { Id = id }, UnitOfWork);
        }
    }

    public async Task<IReadOnlyList<CustomerAddress>> GetDefaultsForUpdateAsync(
        IEnumerable<int> customerIds, CancellationToken cancellationToken)
    {
        int[] ids = customerIds.Distinct().ToArray();
        return await EntityContext.CustomerAddresses
            .Where(address => ids.Contains(address.CustomerId) && (address.IsDefaultBilling || address.IsDefaultShipping))
            .OrderBy(address => address.CustomerId)
            .ThenBy(address => address.Id)
            .ToListAsync(cancellationToken);
    }

    #endregion
}
