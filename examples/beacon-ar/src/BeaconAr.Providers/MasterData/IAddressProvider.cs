using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Entities;
using Paradigm.Enterprise.Providers;

namespace BeaconAr.Providers.MasterData;

public interface IAddressProvider : IProvider
{
    Task<PageResult<CustomerAddressView>> SearchAsync(AddressSearchRequest request, CancellationToken cancellationToken);

    Task<CustomerAddressView> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<CustomerAddressView> CreateAsync(AddressCreateRequest request, CancellationToken cancellationToken);

    Task<CustomerAddressView> UpdateAsync(int id, AddressUpdateRequest request, string expectedVersion, CancellationToken cancellationToken);

    Task DeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken);
}
