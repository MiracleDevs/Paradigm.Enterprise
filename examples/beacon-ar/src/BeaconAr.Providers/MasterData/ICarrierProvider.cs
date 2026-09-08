using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Entities;
using Paradigm.Enterprise.Providers;

namespace BeaconAr.Providers.MasterData;

public interface ICarrierProvider : IProvider
{
    Task<PageResult<CarrierView>> SearchAsync(CarrierSearchRequest request, CancellationToken cancellationToken);

    Task<CarrierView> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<CarrierView> CreateAsync(CarrierCreateRequest request, CancellationToken cancellationToken);

    Task<CarrierView> UpdateAsync(int id, CarrierUpdateRequest request, string expectedVersion, CancellationToken cancellationToken);

    Task DeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken);
}
