using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Entities;
using Paradigm.Enterprise.Domain.Repositories;

namespace BeaconAr.Domain.MasterData.Repositories;

public interface ICarrierViewRepository : IReadRepository<CarrierView, int>
{
    Task<CarrierView?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<PageResult<CarrierView>> SearchAsync(CarrierSearchRequest request, CancellationToken cancellationToken);
}
