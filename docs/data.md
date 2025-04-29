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
  - Added in version 1.0.11: Support for configurable command timeout when executing stored procedures

### Extensions

Helper extension methods for data access operations:

- **DataReaderExtensions** - Extends IDataReader with type conversion methods
  - Added in version 1.0.7: `GetArray` method to read values from database array fields
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

// Example of executing a stored procedure with timeout (since v1.0.11)
public async Task<List<ProductSummary>> GetProductSummariesAsync()
{
    var executor = new StoredProcedureExecutor(_connection);
    
    // Set command timeout to 2 minutes
    var result = await executor.ExecuteReaderAsync<ProductSummary>(
        "GetProductSummaries",
        commandTimeout: 120);
        
    return result.ToList();
}

// Example of reading array data (since v1.0.7)
public async Task<List<Product>> GetProductsWithTagsAsync()
{
    var products = new List<Product>();
    
    using (var reader = await _connection.ExecuteReaderAsync("SELECT Id, Name, Tags FROM Products"))
    {
        while (await reader.ReadAsync())
        {
            var product = new Product
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Tags = reader.GetArray<string>(2) // Read string array from PostgreSQL
            };
            
            products.Add(product);
        }
    }
    
    return products;
}
```

## Database Providers

The Data project is designed to be database-agnostic, with specific implementations provided by:

- **Paradigm.Enterprise.Data.SqlServer** - Microsoft SQL Server provider
- **Paradigm.Enterprise.Data.PostgreSql** - PostgreSQL provider
  - Updated in version 1.0.9: Automatically creates a transaction when executing a stored procedure

## NuGet Package

```
Install-Package Paradigm.Enterprise.Data
``` 