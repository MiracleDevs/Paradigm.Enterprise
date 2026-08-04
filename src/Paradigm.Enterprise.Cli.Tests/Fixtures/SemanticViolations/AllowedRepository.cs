using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Domain.Repositories;

namespace SemanticViolations;

public sealed class AllowedRepository(
    OrdersContext context,
    DbConnection connection,
    RealOrderStoredProcedure sqlServerProcedure,
    PostgreSqlOrderStoredProcedure postgreSqlProcedure,
    BusinessExecutor executor) : IRepository
{
    #region Constants

    private const string RoutineObjectName = "dbo.SearchOrders";
    private const string LogMessage = "Execute the reviewed order search.";

    #endregion

    #region Public Methods

    public void Dispose()
    {
    }

    public async Task<IReadOnlyList<Order>> LinqAndTypedRoutinesAsync()
    {
        context.Orders.Add(new Order());
        await context.SaveChangesAsync();
        var rows = await context.Orders.Where(order => order.Status == "new").OrderBy(order => order.Id).ToListAsync();
        await sqlServerProcedure.ExecuteAsync(connection, new OrderSearchParameters());
        await postgreSqlProcedure.ExecuteAsync(connection, new OrderSearchParameters());
        await executor.ExecuteAsync(RoutineObjectName + LogMessage);
        await executor.QueryAsync(RoutineObjectName);
        await executor.QueryRowsAsync("SELECT Id FROM dbo.BusinessRows");
        return rows;
    }

    #endregion
}
