# Database providers

The SQL Server, PostgreSQL, and MySQL packages adapt the core data library to their respective ADO.NET and Entity Framework providers. Each supplies a connection provider, a `RegisterContext<TContext>` extension, stored-procedure bases, and parameter mapper registration while reusing the core reader mappers, repositories, and Unit of Work.

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

The mapper generator selects SQL Server by default. Use `--database-provider postgresql` or `--database-provider mysql` to generate the corresponding provider's parameter mappers and registration. Handwritten PostgreSQL mappers implement `INpgsqlParameterMapper` and use `NpgsqlParameterMapperFactory`; handwritten MySQL mappers implement `IMySqlParameterMapper` and use `MySqlParameterMapperFactory`. See [Code generation](code-generator.md) for setup and mapping boundaries.

Use stored procedures when the database contract or query characteristics justify them. A simple Entity Framework query does not need to be converted merely because the package supports procedures.

## MySQL with Oracle Connector/NET

`Paradigm.Enterprise.Data.MySql` targets .NET 10 and uses Oracle `MySql.EntityFrameworkCore` 10.0.12 with `MySql.Data` 26.10.0. The initial integration baseline is MySQL 8.4/InnoDB. Keep Oracle's `MySql.Data.MySqlClient` connection, parameter, and transaction types together; the adapter's public surface uses those types.

```csharp
using Paradigm.Enterprise.Data.MySql.Context;
using Paradigm.Enterprise.Data.MySql.Extensions;

builder.Services.AddScoped<MySqlDbConnectionProvider>();
builder.Services.RegisterContext<CatalogDbContext>("CatalogDatabase");
```

Supply `ConnectionStrings:CatalogDatabase` through external configuration. Registration calls `UseMySQL(connection)` without opening the connection. Each scope caches a separate connection per name, owned and disposed by `MySqlDbConnectionProvider`. Application contexts continue deriving from `DbContextBase<TId>`; keep the existing Unit of Work and logged-user registrations. When importing several engines' extensions, use an explicit static registration call if `RegisterContext` becomes ambiguous.

Create a parameter mapper and register a factory once during startup:

```csharp
using MySql.Data.MySqlClient;
using Paradigm.Enterprise.Data.MySql.StoredProcedures;
using Paradigm.Enterprise.Data.MySql.StoredProcedures.Mappers;

MySqlParameterMapperFactory.RegisterMapper<ArchiveParameters>(
    () => new ArchiveParameterMapper());

sealed record ArchiveParameters(int ItemId);

sealed class ArchiveParameterMapper : MySqlParameterMapperBase
{
    protected override void AddMySqlParameters(object parameters)
    {
        var value = (ArchiveParameters)parameters;
        AddMySqlParameter(new MySqlParameter("@ItemId", MySqlDbType.Int32)
        {
            Value = value.ItemId
        });
    }
}

sealed class ArchiveProcedure : StoredProcedureBase<ArchiveParameters>
{
    protected override string StoredProcedureName => "ArchiveItem";
    protected override int? ExecutionTimeout => 30;
}
```

The database project owns `ArchiveItem` and its matching input parameter. Invoke the wrapper from an application repository with the scoped context's connection and optional Unit of Work. Null parameter values become `DBNull.Value`; supply a constructed parameter for explicit type, length, precision, scale, or direction. Factories should create a fresh mapper for each call; duplicate registration keeps the first factory. Typed OUT/INOUT-to-object binding is not part of this API.

`ResultStoredProcedureBase<TParameters, TResult1, ...>` supports one through sixteen ordered results. Pagination procedures return metadata first and rows second. Register nonprimitive result and list-element mappers with the shared `DataReaderMapperFactory`; `List<T>` is the supported concrete collection shape. The scalar shortcut applies only to CLR primitive types, so strings, decimals, dates, Guid values, and nullable primitives require explicit result mappers. Empty object/scalar results return default and supported lists return an empty list.

The procedure helper never disposes its caller's connection. Once execution starts, without an active Unit of Work transaction it closes the connection after success or failure, including one already open on entry. With an active transaction it leaves the connection open and enlists the command. Readers, commands, and attached parameters are cleaned up even when execution or result mapping fails. Application parameter mapping runs before execution; a missing mapper or a parameter-mapping/disposal failure leaves the connection state unchanged. Save through the shared audit-aware `SaveChangesAsync(CancellationToken)`/Unit of Work path; inherited synchronous and Boolean save overloads do not run that audit hook.

A positive `ExecutionTimeout` sets an operation deadline in seconds after the connection opens, covering command execution and result consumption. The adapter cancels an overdue server query and throws `TimeoutException`, including when MySQL reports a normal result after interruption. It disables Oracle's native command timer for that path because the selected driver can ignore or hang during asynchronous timeout recovery. A null timeout keeps the driver's default; zero means infinite. A synchronous result mapper cannot be preempted, but a result completed after the deadline is rejected. The helper finishes cancellation before returning the connection so an expired timer cannot interrupt a subsequent command. If server cancellation or the underlying I/O cannot finish, returning safely can take longer than the deadline; this is not a hard transport-abort guarantee.

MySQL schemas are databases, so port `dbo`/PostgreSQL schema annotations deliberately. For Guid values, Oracle natively maps `char(36)` with `OldGuids=False`; use `HasColumnType("char(36)")` and retain the Guid CLR type. Adding `HasConversion<string>()` to that native mapping conflicts with Oracle's store-type mapping. Map instants as UTC `datetime(6)` with explicit conversion; arbitrary offsets require separate storage. Validate Boolean/BIT, unsigned numbers, collations, zero-date handling, and JSON representations for the application model. MySQL DDL can implicitly commit and belongs outside runtime transactions. All contexts and procedure commands in one atomic operation must share the same connection. See [Transactions](guides/transactions.md).

Provider exceptions propagate to the application's existing error boundary; the adapter adds no automatic retries or SQL Server exception translation. Configure least-privilege access and TLS in the deployment connection settings. The CLI generates MySQL mappers with `generate mappers --database-provider mysql`; custom type metadata still needs review. Database publishing, Aspire bootstrap, MariaDB certification, and cross-engine data migration are outside this adapter. The Oracle dependencies retain their `GPL-2.0-only WITH Universal-FOSS-exception-1.0` license terms; the framework's MIT metadata does not replace them.
