using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.Sales.Contracts;
using BeaconAr.Domain.Sales.Entities;
using Paradigm.Enterprise.Providers;

namespace BeaconAr.Providers.Sales;

public interface ISalesOrderProvider : IProvider
{
    Task<PageResult<SalesOrderView>> SearchAsync(SalesOrderSearchRequest request, CancellationToken cancellationToken);
    Task<SalesOrderDto> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<SalesOrderDto> CreateDirectAsync(SalesOrderCreateRequest request, CancellationToken cancellationToken);
    Task<SalesOrderDto> UpdateAsync(int id, SalesOrderUpdateRequest request, string expectedVersion, CancellationToken cancellationToken);
    Task DeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken);
    Task<SalesOrderDto> TransitionAsync(int id, SalesOrderStatusTransitionRequest request, string expectedVersion, CancellationToken cancellationToken);
}
