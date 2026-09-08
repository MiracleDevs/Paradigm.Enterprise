using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Entities;
using Paradigm.Enterprise.Domain.Repositories;

namespace BeaconAr.Domain.MasterData.Repositories;

public interface IAddressViewRepository : IReadRepository<CustomerAddressView, int>
{
    Task<CustomerAddressView?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<PageResult<CustomerAddressView>> SearchAsync(AddressSearchRequest request, CancellationToken cancellationToken);
}
