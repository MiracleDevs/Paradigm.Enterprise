using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.Receivables.Entities;
using Paradigm.Enterprise.Providers;

namespace BeaconAr.Providers.MasterData;

public interface ICustomerProvider : IEditProvider<CustomerView, int>
{
    Task<PageResult<CustomerDto>> SearchAsync(CustomerSearchRequest request, CancellationToken cancellationToken);

    Task<CustomerDto> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<CustomerDto> CreateAsync(CustomerCreateRequest request, CancellationToken cancellationToken);

    Task<CustomerDto> UpdateAsync(int id, CustomerUpdateRequest request, string expectedVersion, CancellationToken cancellationToken);

    Task DeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken);
}
