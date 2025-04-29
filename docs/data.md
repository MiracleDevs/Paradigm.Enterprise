# Paradigm.Enterprise.Data

The Data project provides a flexible and extensible data access layer for the Paradigm.Enterprise framework. It includes implementations of repositories, unit of work, and database context abstractions to support various database providers.

## Key Components

### Repositories

The Data project implements the repository pattern through several base classes:

- **RepositoryBase<T>** - Base implementation of the `IRepository<T>` interface
- **AuditableRepositoryBase<T>** - Repository implementation with auditing support
- **QueryableRepositoryBase<T>** - Repository with enhanced querying capabilities

### Unit of Work

The unit of work implementation manages transactions and synchronizes changes:

- **UnitOfWorkBase** - Base implementation of `IUnitOfWork`
- **EfUnitOfWork** - Entity Framework-specific unit of work implementation

### Database Context

The project provides database context abstractions:

- **IDataContext** - Interface for database context operations
- **EfDataContext** - Entity Framework implementation of the data context

### Stored Procedures

The Data project includes utilities for working with stored procedures:

- **StoredProcedureBuilder** - Builds SQL for stored procedure execution
- **StoredProcedureExecutor** - Executes stored procedures and processes results

### Extensions

Helper extension methods for data access operations:

- **DataReaderExtensions** - Extends IDataReader with type conversion methods
- **QueryableExtensions** - Extends IQueryable with sorting and filtering capabilities

## Usage Example

```csharp
// Example of configuring services in ASP.NET Core
public void ConfigureServices(IServiceCollection services)
{
    // Register data services
    services.AddScoped<IDataContext, EfDataContext>();
    services.AddScoped<IUnitOfWork, EfUnitOfWork>();
    
    // Register repositories
    services.AddScoped<IRepository<Product>, RepositoryBase<Product>>();
    services.AddScoped<IAuditableRepository<Customer>, AuditableRepositoryBase<Customer>>();
}

// Example of using the repository and unit of work
public class ProductService
{
    private readonly IRepository<Product> _repository;
    private readonly IUnitOfWork _unitOfWork;
    
    public ProductService(IRepository<Product> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<Product> CreateAsync(Product product)
    {
        await _repository.CreateAsync(product);
        await _unitOfWork.CommitAsync();
        return product;
    }
    
    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }
    
    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }
}
```

## Database Providers

The Data project is designed to be database-agnostic, with specific implementations provided by:

- **Paradigm.Enterprise.Data.SqlServer** - Microsoft SQL Server provider
- **Paradigm.Enterprise.Data.PostgreSql** - PostgreSQL provider

## NuGet Package

```
Install-Package Paradigm.Enterprise.Data
``` 