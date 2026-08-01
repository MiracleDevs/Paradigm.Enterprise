using BeaconAr.Domain.MasterData.Contracts;
using Paradigm.Enterprise.Domain.Repositories;

namespace BeaconAr.Domain.MasterData.Repositories;

public interface ICustomerViewRepository : IRepository
{
    Task<CustomerDto?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<PageResult<CustomerDto>> SearchAsync(CustomerSearchRequest request, CancellationToken cancellationToken);
}
