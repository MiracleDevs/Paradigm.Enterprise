using BeaconAr.Domain.MasterData.Contracts;
using Paradigm.Enterprise.Domain.Repositories;

namespace BeaconAr.Domain.MasterData.Repositories;

public interface ICarrierViewRepository : IRepository
{
    Task<CarrierDto?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<PageResult<CarrierDto>> SearchAsync(CarrierSearchRequest request, CancellationToken cancellationToken);
}
