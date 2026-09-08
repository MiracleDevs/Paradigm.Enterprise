using BeaconAr.Data.Sales.Context;
using BeaconAr.Data.Sales.StoredProcedures;
using BeaconAr.Domain.Sales.Entities;
using BeaconAr.Domain.Sales.Repositories;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Repositories;

namespace BeaconAr.Data.Sales.Repositories;

public sealed class SalesOrderRepository : RepositoryBase<SalesDbContext, int>, ISalesOrderRepository
{
    #region Fields

    private static readonly AllocateSalesOrderNumberProcedure AllocateNumberProcedure = new();

    #endregion

    #region Constructors

    public SalesOrderRepository(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    #endregion

    #region Public Methods

    public async Task<string> AllocateNumberAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        long value = await AllocateNumberProcedure.ExecuteAsync(GetDbConnection(), new AllocateSalesOrderNumberParameters(), UnitOfWork);
        cancellationToken.ThrowIfCancellationRequested();
        return SalesNumberFormatter.Order(value);
    }

    public Task<SalesOrder?> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        EntityContext.SalesOrders.Include(order => order.SalesOrderLines)
            .SingleOrDefaultAsync(order => order.Id == id && order.DeletionDate == null, cancellationToken);

    public Task<SalesOrder?> FindBySourceQuoteAsync(int sourceQuoteId, CancellationToken cancellationToken) =>
        EntityContext.SalesOrders.Include(order => order.SalesOrderLines)
            .SingleOrDefaultAsync(order => order.SourceQuoteId == sourceQuoteId, cancellationToken);

    public void Add(SalesOrder salesOrder) => EntityContext.SalesOrders.Add(salesOrder);

    public void ReplaceLines(SalesOrder salesOrder, IReadOnlyCollection<SalesOrderLine> lines)
    {
        EntityContext.SalesOrderLines.RemoveRange(salesOrder.SalesOrderLines);
        salesOrder.SalesOrderLines.Clear();
        foreach (SalesOrderLine line in lines)
            salesOrder.SalesOrderLines.Add(line);
    }

    public void AddHistory(SalesOrderStatusHistory history) => EntityContext.SalesOrderStatusHistories.Add(history);

    #endregion
}
