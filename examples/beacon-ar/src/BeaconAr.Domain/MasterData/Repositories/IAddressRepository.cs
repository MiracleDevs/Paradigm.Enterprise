using BeaconAr.Domain.Receivables.Entities;
using BeaconAr.Domain.MasterData.Contracts;
using Paradigm.Enterprise.Domain.Repositories;

namespace BeaconAr.Domain.MasterData.Repositories;

public interface IAddressRepository : IRepository
{
    Task<CustomerAddress?> GetForUpdateAsync(int id, CancellationToken cancellationToken);

    Task<int?> GetActiveTypeIdAsync(string code, CancellationToken cancellationToken);

    Task<bool> HasReferencesAsync(int id, CancellationToken cancellationToken);

    Task LockCustomersAsync(IEnumerable<int> customerIds, CancellationToken cancellationToken);

    Task ReparentAsync(CustomerAddress address, AddressUpdateRequest request, int addressTypeId, int userId,
        DateTimeOffset now, CancellationToken cancellationToken);

    Task<IReadOnlyList<CustomerAddress>> GetDefaultsForUpdateAsync(IEnumerable<int> customerIds, CancellationToken cancellationToken);

    void Add(CustomerAddress address);

    void Delete(CustomerAddress address);
}
