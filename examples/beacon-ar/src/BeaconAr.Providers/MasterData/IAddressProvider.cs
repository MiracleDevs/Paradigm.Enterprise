using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.Receivables.Entities;
using Paradigm.Enterprise.Providers;

namespace BeaconAr.Providers.MasterData;

public interface IAddressProvider : IEditProvider<CustomerAddressView, int>
{
    Task<PageResult<AddressDto>> SearchAsync(AddressSearchRequest request, CancellationToken cancellationToken);

    Task<AddressDto> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<AddressDto> CreateAsync(AddressCreateRequest request, CancellationToken cancellationToken);

    Task<AddressDto> UpdateAsync(int id, AddressUpdateRequest request, string expectedVersion, CancellationToken cancellationToken);

    Task DeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken);
}
