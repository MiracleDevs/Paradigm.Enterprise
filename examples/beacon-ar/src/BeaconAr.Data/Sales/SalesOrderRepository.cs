using BeaconAr.Data.Receivables;
using BeaconAr.Domain.Receivables.Generated;
using BeaconAr.Domain.Sales.Repositories;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Repositories;
using System.Data;

namespace BeaconAr.Data.Sales;

public sealed class SalesOrderRepository : RepositoryBase<ReceivablesDbContext, int>, ISalesOrderRepository
{
    #region Constructors

    public SalesOrderRepository(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    #endregion

    #region Public Methods

    public async Task<string> AllocateNumberAsync(CancellationToken cancellationToken)
    {
        var connection = GetDbConnection();
        if (connection.State == ConnectionState.Closed)
            await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT NEXT VALUE FOR [dbo].[SalesOrderNumberSequence]";
        command.CommandType = CommandType.Text;
        command.CommandTimeout = 30;
        UnitOfWork.UseTransaction(command);
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        long value = Convert.ToInt64(result, System.Globalization.CultureInfo.InvariantCulture);
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
