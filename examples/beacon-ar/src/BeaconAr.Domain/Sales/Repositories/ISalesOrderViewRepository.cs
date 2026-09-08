using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.Sales.Contracts;
using BeaconAr.Domain.Sales.Entities;
using Paradigm.Enterprise.Domain.Repositories;

namespace BeaconAr.Domain.Sales.Repositories;

public interface ISalesOrderViewRepository : IRepository
{
    Task<SalesOrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<SalesOrderDto?> GetByIdIncludingDeletedAsync(int id, CancellationToken cancellationToken);

    Task<PageResult<SalesOrderView>> SearchAsync(SalesOrderSearchRequest request, CancellationToken cancellationToken);
}
