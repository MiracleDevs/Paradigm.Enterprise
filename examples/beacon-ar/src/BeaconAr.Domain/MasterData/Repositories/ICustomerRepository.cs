using BeaconAr.Domain.Receivables.Generated;
using Paradigm.Enterprise.Domain.Repositories;

namespace BeaconAr.Domain.MasterData.Repositories;

public interface ICustomerRepository : IRepository
{
    Task<Customer?> GetForUpdateAsync(int id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);

    Task<bool> AccountNumberExistsAsync(string accountNumber, int? excludedId, CancellationToken cancellationToken);

    Task<bool> HasReferencesAsync(int id, CancellationToken cancellationToken);

    void Add(Customer customer);

    void Delete(Customer customer);
}
