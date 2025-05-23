# 1. Paradigm.Enterprise Database Providers

The Paradigm.Enterprise framework includes specialized database providers that extend the core Data library for specific database platforms. These providers implement database-specific functionality while adhering to the common abstraction defined in the Data project.

## 1.1. SQL Server Provider (Paradigm.Enterprise.Data.SqlServer)

The SQL Server provider is an implementation specific to Microsoft SQL Server databases.

### 1.1.1. Key Components

- **SqlServerDataContext** - SQL Server-specific implementation of the data context
- **SqlServerRepositoryBase\<T>** - Repository implementation optimized for SQL Server
- **SqlServerUnitOfWork** - Unit of work implementation for SQL Server
- **SqlServerParameterMapper** - Maps .NET types to SQL Server parameter types
- **SqlServerStoredProcedureExecutor** - Executes stored procedures on SQL Server

### 1.1.2. Usage Example

```csharp
// Registering SQL Server services in ASP.NET Core
public void ConfigureServices(IServiceCollection services)
{
    // Add SQL Server connection
    services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

    // Register SQL Server data services
    services.AddScoped<IDataContext, SqlServerDataContext>();
    services.AddScoped<IUnitOfWork, SqlServerUnitOfWork>();

    // Register repositories
    services.AddScoped<IRepository<Product>, SqlServerRepositoryBase<Product>>();
}
```

### 1.1.3. NuGet Package

```shell
Install-Package Paradigm.Enterprise.Data.SqlServer
```

## 1.2. PostgreSQL Provider (Paradigm.Enterprise.Data.PostgreSql)

The PostgreSQL provider is an implementation specific to PostgreSQL databases.

### 1.2.1. Key Components

- **PostgreSqlDataContext** - PostgreSQL-specific implementation of the data context
- **PostgreSqlRepositoryBase\<T>** - Repository implementation optimized for PostgreSQL
- **PostgreSqlUnitOfWork** - Unit of work implementation for PostgreSQL
- **PostgreSqlParameterMapper** - Maps .NET types to PostgreSQL parameter types
- **PostgreSqlStoredProcedureExecutor** - Executes stored procedures on PostgreSQL
  - Updated in version 1.0.9: Automatically creates a transaction when executing stored procedures to ensure data consistency

### 1.2.2. Usage Example

```csharp
// Registering PostgreSQL services in ASP.NET Core
public void ConfigureServices(IServiceCollection services)
{
    // Add PostgreSQL connection
    services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));

    // Register PostgreSQL data services
    services.AddScoped<IDataContext, PostgreSqlDataContext>();
    services.AddScoped<IUnitOfWork, PostgreSqlUnitOfWork>();

    // Register repositories
    services.AddScoped<IRepository<Product>, PostgreSqlRepositoryBase<Product>>();
}

// Example of executing a stored procedure with automatic transaction (since v1.0.9)
public async Task<List<Product>> GetProductsInCategoryAsync(int categoryId)
{
    var executor = new PostgreSqlStoredProcedureExecutor(_connection);

    // Transaction will be automatically created
    var result = await executor.ExecuteReaderAsync<Product>(
        "get_products_in_category",
        new[] { new NpgsqlParameter("p_category_id", categoryId) });

    return result.ToList();
}
```

### 1.2.3. NuGet Package

```shell
Install-Package Paradigm.Enterprise.Data.PostgreSql
```

## 1.3. Common Features

Both providers share these common features:

1. **Connection Management** - Efficient handling of database connections
2. **Parameter Mapping** - Automatic mapping of .NET types to database-specific parameters
3. **Command Generation** - Optimized SQL command generation for performance
4. **Transaction Support** - Transaction management through the Unit of Work pattern
5. **Entity Tracking** - Efficient change tracking for modified entities
6. **Auditing Support** - Built-in support for entity auditing

## 1.4. Extending to Other Databases

The framework's modular design allows for extending support to other database platforms by:

1. Creating a new project (e.g., `Paradigm.Enterprise.Data.MySql`)
2. Implementing the interfaces defined in the Data project
3. Providing database-specific implementations of context, repositories, and unit of work
