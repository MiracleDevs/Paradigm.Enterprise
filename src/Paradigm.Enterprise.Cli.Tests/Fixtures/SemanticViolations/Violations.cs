using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Context;
using Paradigm.Enterprise.Data.Repositories;
using Paradigm.Enterprise.Domain.Dtos;
using Paradigm.Enterprise.Domain.Entities;
using Paradigm.Enterprise.Domain.Repositories;

namespace SemanticViolations;

public sealed class Order : EntityBase<int>
{
    public string Status { get; set; } = "new";

    public void Activate() => Status = "active";
}

public sealed class OrdersContext(DbContextOptions options, IServiceProvider services)
    : DbContextBase<int>(services, options)
{
    public DbSet<Order> Orders => Set<Order>();
}

public interface IOrderRepository : IReadRepository<Order, int>;

public sealed class OrderRepository(IServiceProvider services)
    : ReadRepositoryBase<Order, OrdersContext, int>(services), IOrderRepository
{
    protected override Func<PaginationParametersBase, Task<(PaginationInfo, List<Order>)>>
        GetSearchPaginatedFunction(PaginationParametersBase parameters) =>
        async input =>
        {
            var page = input.PageNumber ?? 1;
            var size = input.PageSize ?? 10;
            var query = EntityContext.Orders.OrderBy(x => x.Id);
            var count = await query.CountAsync();
            var rows = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();
            return (new PaginationInfo
            {
                ItemsCount = count,
                PageNumber = page,
                TotalPages = (int)Math.Ceiling(count / (double)size)
            }, rows);
        };
}

public sealed class OrderService
{
    public void BypassBehavior(Order order)
    {
        // This deliberately bypasses Order.Activate() for the semantic check fixture.
        order.Status = "active";
    }
}
