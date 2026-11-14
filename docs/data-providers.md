# Database providers

The SQL Server, PostgreSQL, MySQL, and Oracle packages adapt the core data library to their respective ADO.NET and Entity Framework providers. Each supplies a connection provider, a `RegisterContext<TContext>` extension, stored-procedure bases, and parameter mapper registration while reusing the core reader mappers, repositories, and Unit of Work.

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

The mapper generator selects SQL Server by default. Use `--database-provider postgresql`, `--database-provider mysql`, or `--database-provider oracle` to generate the corresponding provider's parameter mappers and registration. Handwritten PostgreSQL mappers implement `INpgsqlParameterMapper` and use `NpgsqlParameterMapperFactory`; handwritten MySQL mappers implement `IMySqlParameterMapper` and use `MySqlParameterMapperFactory`; handwritten Oracle mappers implement `IOracleParameterMapper` and use `OracleParameterMapperFactory`. See [Code generation](code-generator.md) for setup and mapping boundaries.

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

Provider exceptions propagate to the application's existing error boundary; the adapter adds no automatic retries or SQL Server exception translation. Configure least-privilege access and TLS in the deployment connection settings. The CLI generates MySQL mappers with `generate mappers --database-provider mysql`; custom type metadata still needs review. Database publishing, Aspire bootstrap, MariaDB certification, and cross-engine data migration are outside this adapter. The Oracle MySQL dependencies retain their `GPL-2.0-only WITH Universal-FOSS-exception-1.0` license terms; the framework's MIT metadata does not replace them.

## Oracle Database with ODP.NET

`Paradigm.Enterprise.Data.Oracle` targets .NET 10 using official `Oracle.EntityFrameworkCore` 10.23.26400 and `Oracle.ManagedDataAccess.Core` 23.26.400. This is the Oracle Database adapter; `Data.MySql` uses Oracle's separate MySQL products. The Oracle package references abstract Data directly, retaining the existing contexts, repositories, audit behavior, and Unit of Work contracts.

```csharp
using Paradigm.Enterprise.Data.Oracle.Context;
using Paradigm.Enterprise.Data.Oracle.Extensions;

builder.Services.AddScoped<OracleDbConnectionProvider>();
builder.Services.RegisterContext<CatalogDbContext>("CatalogDatabase");
```

Supply `ConnectionStrings:CatalogDatabase` through external configuration, for example using an Oracle service name in `Data Source=host:1521/service`. Registration configures `UseOracle` without opening the connection. Each scope owns one lazy connection per configured name. Keep the existing logged-user and Unit of Work registrations and derive application contexts from `DbContextBase<TId>`. Multiple imported provider extension namespaces may require an explicit static `RegisterContext` call. Applications targeting older Oracle releases must explicitly configure the provider's SQL compatibility and model representations.

Oracle result procedures declare explicit output cursors. Alias returned columns to match CLR property names exactly, including casing: shared mapper field validation is case-sensitive against schema metadata, while unquoted Oracle names become uppercase. For example, the database project owns:

```sql
CREATE OR REPLACE PROCEDURE FindItem(
    ItemId IN NUMBER,
    p_result1 OUT SYS_REFCURSOR)
AS
BEGIN
    OPEN p_result1 FOR
        SELECT ID AS "Id", NAME AS "Name" FROM CATALOG_ITEM WHERE ID = ItemId;
END;
```

Its typed wrapper uses the matching result count:

```csharp
using Paradigm.Enterprise.Data.Oracle.StoredProcedures;

sealed record FindItemParameters(int ItemId);

sealed class FindItemProcedure : ResultStoredProcedureBase<FindItemParameters, CatalogItemView>
{
    protected override string StoredProcedureName => "FindItem";
    protected override int? ExecutionTimeout => 30;
}
```

Generate its input/reader mappers with `generate mappers --database-provider oracle`, then call the generated registration at startup. For handwritten inputs, derive from `OracleParameterMapperBase`, implement `AddOracleParameters(object)`, and add an `OracleParameter("ItemId", OracleDbType.Int32)` with the input value. Register a fresh factory with `OracleParameterMapperFactory.RegisterMapper<FindItemParameters>`. Parameter names are unprefixed and commands bind by name. Null inputs become `DBNull.Value`; constructed parameters allow explicit length, precision, scale, and direction.

Each result base automatically appends output `OracleDbType.RefCursor` parameters named `p_result1` through `p_result16`. To match an existing signature, override `GetResultParameterName(int resultIndex)` using its one-based position. Do not also add these parameters in an input mapper. Tuples follow cursor binding order. Every declared cursor must be opened; an opened cursor containing zero rows retains its position, while NULL/unopened cursors are outside this contract. Implicit results through `DBMS_SQL.RETURN_RESULT`, scalar function returns, and automatic scalar OUT/INOUT-to-object binding are outside the adapter's initial API.

Use the shared `DataReaderMapperFactory` for object, nonprimitive scalar, and list-element results. Oracle NUMBER values often arrive as `decimal`; the Oracle routine helper normalizes primitive numeric results, rejecting fractional integral values and overflow. Numeric Boolean scalar results must be zero or one. Generated readers use ODP.NET-compatible typed getters; character results require exactly one character. Generated Boolean inputs use NUMBER(1)-compatible Int16 values, while reader mappings accept NUMBER and native BOOLEAN. Review generated types and database precision together. Oracle treats empty character strings as null. Use explicit NUMBER precision/scale, RAW(16) Guid storage with consistent .NET byte order, and deliberate TIMESTAMP/UTC policies. Native SQL BOOLEAN depends on server compatibility. The selected Oracle EF release does not support DateOnly or TimeOnly. Oracle unquoted identifiers fold to uppercase, and schemas correspond to users.

Connections remain caller-owned during routine execution. Nontransactional connections close after execution success or failure, including those open on entry; active Unit of Work connections remain open. Parameter-mapping failures before execution preserve the entry state. All participating contexts and routines must use the exact same `OracleConnection`. The adapter validates the transaction supplied through the Unit of Work because Oracle's `IDbCommand.Transaction` setter does not enforce enlistment. Routines must not commit, roll back, execute DDL, or use autonomous transactions inside application-owned transactions. See [Transactions](guides/transactions.md).

`ExecutionTimeout` configures ODP.NET's command timeout in seconds; null retains the driver default and zero is unlimited. It is a provider command timeout, not an application mapping deadline. Provider errors propagate without retries or SQL Server exception translation. Dispose readers/commands and release parameters before reusing connections. Configure least-privilege users and transport security in the deployment environment.

Oracle dependencies use the [Oracle Free Distribution, Hosting, and Use terms](https://www.nuget.org/packages/Oracle.EntityFrameworkCore/10.23.26400/License), distinct from both the framework's MIT license and MySQL's dependency terms. Oracle database publishing, Aspire hosting/bootstrap, production authentication recipes, UDT/collection parameters, and bulk/array binding remain separate capabilities.
