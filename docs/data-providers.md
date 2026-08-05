# Database providers

The SQL Server and PostgreSQL packages adapt the core data library to their respective ADO.NET and Entity Framework providers. Both supply a connection provider, a `RegisterContext<TContext>` extension, stored-procedure bases, result-set readers, and parameter mapper registration.

## Registering a context

Register the database-specific connection provider before calling `RegisterContext`. The extension receives the name of a connection string, not the connection string value.

```csharp
using Paradigm.Enterprise.Data.SqlServer.Context;
using Paradigm.Enterprise.Data.SqlServer.Extensions;

builder.Services.AddScoped<SqlServerDbContextConnectionProvider>();
builder.Services.RegisterContext<CatalogDbContext>("CatalogDatabase");
```

The corresponding configuration is resolved through `IConfiguration.GetConnectionString`.

```json
{
  "ConnectionStrings": {
    "ApplicationDatabase": "Server=(localdb)\\MSSQLLocalDB;Database=Sample;Integrated Security=true"
  }
}
```

Do not store production credentials in committed settings. Use environment variables, user secrets for local development, or the secret-management facility of the deployment platform.

For PostgreSQL, register `PostgreSqlDbConnectionProvider` and use the PostgreSQL extension namespace. The application context and repository layering remain the same.

## Stored procedures

Stored-procedure classes inherit the provider-specific `StoredProcedureBase<TParameters>` or a `ResultStoredProcedureBase` matching the number of result sets. Parameter and data-reader mappers translate between CLR types and database values.

SQL Server multi-result tuples follow database result-set order. PostgreSQL multi-result procedures instead map an implementation-defined enumeration of distinct returned cursor names; tuple positions do not promise database return order. Use compatible result shapes or identify business meaning explicitly rather than assuming cursor position.

The current standalone mapper generator emits SQL Server parameter mappers and registers them through `SqlParameterMapperFactory`; it does not generate PostgreSQL parameter mappers. PostgreSQL applications must implement `INpgsqlParameterMapper` and register it through `NpgsqlParameterMapperFactory`. The standalone generator also has a path-handling defect that prevents its stored-procedure mode from completing, so do not depend on it until that limitation is fixed. See [Code generation](code-generator.md) for the current status.

Use stored procedures when the database contract or query characteristics justify them. A simple Entity Framework query does not need to be converted merely because the package supports procedures.
