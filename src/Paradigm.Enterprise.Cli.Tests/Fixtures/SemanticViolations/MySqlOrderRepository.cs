using Microsoft.EntityFrameworkCore;

namespace SemanticViolations;

public sealed class MySqlOrderRepository
{
    #region Public Methods

    public async Task<List<Order>> SearchTypedProcedure(
        OrdersContext context,
        MySqlOrderSearchProcedure procedure) =>
        await procedure.ExecuteAsync(context.Database.GetDbConnection(), new OrderSearchParameters()) ?? [];

    public async Task<List<Order>> SearchMixedTypedProcedureAndEf(
        OrdersContext context,
        MySqlOrderSearchProcedure procedure)
    {
        await procedure.ExecuteAsync(context.Database.GetDbConnection(), new OrderSearchParameters());
        return await context.Orders.Skip(2).Take(3).ToListAsync();
    }

    public async Task<List<Order>> SearchMixedCommandProcedureAndEf(
        OrdersContext context,
        MySqlRefreshProcedure procedure)
    {
        await procedure.ExecuteAsync(context.Database.GetDbConnection());
        return await context.Orders.Skip(2).Take(3).ToListAsync();
    }

    public async Task<List<Order>> SearchLookalikeProcedureAndEf(
        OrdersContext context,
        Lookalike.StoredProcedureBase procedure)
    {
        await procedure.ExecuteAsync();
        return await context.Orders.Skip(2).Take(3).ToListAsync();
    }

    #endregion
}