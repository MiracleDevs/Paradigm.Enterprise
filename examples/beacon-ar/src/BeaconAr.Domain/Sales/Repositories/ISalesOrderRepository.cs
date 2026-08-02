using BeaconAr.Domain.Receivables.Entities;
using Paradigm.Enterprise.Domain.Repositories;

namespace BeaconAr.Domain.Sales.Repositories;

public interface ISalesOrderRepository : IRepository
{
    Task<string> AllocateNumberAsync(CancellationToken cancellationToken);

    Task<SalesOrder?> GetForUpdateAsync(int id, CancellationToken cancellationToken);

    Task<SalesOrder?> FindBySourceQuoteAsync(int sourceQuoteId, CancellationToken cancellationToken);

    void Add(SalesOrder salesOrder);

    void ReplaceLines(SalesOrder salesOrder, IReadOnlyCollection<SalesOrderLine> lines);

    void AddHistory(SalesOrderStatusHistory history);
}
