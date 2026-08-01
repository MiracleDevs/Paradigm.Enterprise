using BeaconAr.Domain.MasterData.Contracts;
using Paradigm.Enterprise.Providers;

namespace BeaconAr.Providers.MasterData;

public interface ICustomerProvider : IProvider
{
    Task<PageResult<CustomerDto>> SearchAsync(CustomerSearchRequest request, CancellationToken cancellationToken);

    Task<CustomerDto> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<CustomerDto> CreateAsync(CustomerCreateRequest request, CancellationToken cancellationToken);

    Task<CustomerDto> UpdateAsync(int id, CustomerUpdateRequest request, string expectedVersion, CancellationToken cancellationToken);

    Task DeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken);
}
