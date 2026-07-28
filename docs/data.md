# Data access

`Paradigm.Enterprise.Data` provides Entity Framework repository bases, a context base, Unit of Work implementation, database connection access, and result mapping for stored procedures. Repositories remain application types: the library supplies mechanics, while each application defines repository contracts around its domain and query needs.

## Context

An application context derives from `DbContextBase<TId>` and receives both `IServiceProvider` and `DbContextOptions`. The context implements `ICommiteable`, so repositories can register it with the scoped Unit of Work.

```csharp
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Context;

public sealed class ApplicationDbContext
    : DbContextBase<Guid>
{
    public ApplicationDbContext(
        IServiceProvider serviceProvider,
        DbContextOptions<ApplicationDbContext> options)
        : base(serviceProvider, options)
    {
    }

    public DbSet<CatalogItem> CatalogItems => Set<CatalogItem>();
    public DbSet<CatalogItemView> CatalogItemViews => Set<CatalogItemView>();
}
```

During `SaveChangesAsync`, the context finds entities implementing `IAuditableEntity<TId>`. If `ILoggedUserService<TId>` returns an authenticated user, the context applies audit values before saving. Applications using auditable entities must register the logged-user service or provide their own implementation.

## Read repositories

`ReadRepositoryBase<TEntity, TContext, TId>` supplies `GetAllAsync`, `GetByIdAsync`, and `GetByIdsAsync`. Search calls `GetSearchPaginatedFunction`, which throws unless a derived repository provides an implementation. A repository that exposes search must therefore override this function.

```csharp
public interface ICatalogItemViewRepository
    : IReadRepository<CatalogItemView, Guid>
{
}

public sealed class CatalogItemViewRepository
    : ReadRepositoryBase<CatalogItemView, ApplicationDbContext, Guid>,
      ICatalogItemViewRepository
{
    public CatalogItemViewRepository(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }
}
```

Use a view repository for read projections and a domain repository for editable entities. A database view is one possible implementation, not a requirement.

## Edit repositories

`EditRepositoryBase<TEntity, TContext, TId>` adds tracking operations for add, update, and delete. These methods do not call `SaveChangesAsync`. The provider or another application workflow commits through `IUnitOfWork`.

Repository methods should express data access. Domain decisions remain in entities, while cross-repository sequencing belongs in providers.

## Unit of Work

`RepositoryBase` resolves its context and registers it with `IUnitOfWork` when the repository is constructed. `CommitChangesAsync` then commits every registered context. Basic edit-provider operations call this automatically.

An explicit transaction is needed when several persistence operations must succeed or fail together. See [Transactions](guides/transactions.md) for the required ordering and limitations.
