using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.Sales.Contracts;
using Paradigm.Enterprise.Domain.Repositories;

namespace BeaconAr.Domain.Sales.Repositories;

public interface ISalesOrderViewRepository : IRepository
{
    Task<SalesOrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<SalesOrderDto?> GetByIdIncludingDeletedAsync(int id, CancellationToken cancellationToken);

    Task<PageResult<SalesOrderSummaryDto>> SearchAsync(SalesOrderSearchRequest request, CancellationToken cancellationToken);
}
