using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Paradigm.Enterprise.Data.SqlServer.StoredProcedures;

namespace SemanticViolations;

public sealed class InMemoryOrderRepository
{
    public IReadOnlyList<Order> SearchPage(IReadOnlyList<Order> orders) =>
        orders.Skip(1).Take(2).ToArray();

    public async Task<List<Order>> SearchPlaceholderStoredProcedure(OrdersContext context)
    {
        await OrderStoredProcedure.ExecuteAsync();
        return await context.Orders.Skip(1).Take(2).ToListAsync();
    }

    public async Task<List<Order>> SearchRealStoredProcedure(
        OrdersContext context,
        RealOrderStoredProcedure procedure)
    {
        return await procedure.ExecuteAsync(
            context.Database.GetDbConnection(), new OrderSearchParameters()) ?? [];
    }

    public async Task<List<Order>> SearchMixedStoredProcedureAndEf(
        OrdersContext context,
        RealOrderStoredProcedure procedure)
    {
        await procedure.ExecuteAsync(
            context.Database.GetDbConnection(), new OrderSearchParameters());
        return await context.Orders.Skip(2).Take(3).ToListAsync();
    }
}

public static class OrderStoredProcedure
{
    public static Task ExecuteAsync() => Task.CompletedTask;
}

public sealed record OrderSearchParameters;

public sealed class RealOrderStoredProcedure
    : ResultStoredProcedureBase<OrderSearchParameters, List<Order>>
{
    protected override string StoredProcedureName => "SearchOrders";
}
