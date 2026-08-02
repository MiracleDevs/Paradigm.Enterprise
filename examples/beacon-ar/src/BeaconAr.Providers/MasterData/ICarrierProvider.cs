using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.Receivables.Entities;
using Paradigm.Enterprise.Providers;

namespace BeaconAr.Providers.MasterData;

public interface ICarrierProvider : IEditProvider<CarrierView, int>
{
    Task<PageResult<CarrierDto>> SearchAsync(CarrierSearchRequest request, CancellationToken cancellationToken);

    Task<CarrierDto> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<CarrierDto> CreateAsync(CarrierCreateRequest request, CancellationToken cancellationToken);

    Task<CarrierDto> UpdateAsync(int id, CarrierUpdateRequest request, string expectedVersion, CancellationToken cancellationToken);

    Task DeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken);
}
