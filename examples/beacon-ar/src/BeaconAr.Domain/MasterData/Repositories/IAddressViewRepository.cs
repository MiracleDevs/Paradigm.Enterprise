using BeaconAr.Domain.MasterData.Contracts;
using Paradigm.Enterprise.Domain.Repositories;

namespace BeaconAr.Domain.MasterData.Repositories;

public interface IAddressViewRepository : IRepository
{
    Task<AddressDto?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<PageResult<AddressDto>> SearchAsync(AddressSearchRequest request, CancellationToken cancellationToken);
}
