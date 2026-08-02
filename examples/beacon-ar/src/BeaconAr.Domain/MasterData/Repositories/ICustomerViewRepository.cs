using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.Receivables.Entities;
using Paradigm.Enterprise.Domain.Repositories;

namespace BeaconAr.Domain.MasterData.Repositories;

public interface ICustomerViewRepository : IReadRepository<CustomerView, int>
{
    Task<CustomerView?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<PageResult<CustomerView>> SearchAsync(CustomerSearchRequest request, CancellationToken cancellationToken);
}
