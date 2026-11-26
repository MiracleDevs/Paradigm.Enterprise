# Repository patterns

## Aggregate deletion

Expose removal through the aggregate repository. Use the protected aggregate-removal helpers supplied by the selected base. Do not give callers a child repository solely to bypass the root.

## Search

Implement the protected paginated search function or a focused custom method through a stored-procedure boundary. Apply a stable order in the routine before paging. Treat search parameters as application/data contracts, not HTTP request objects.

## Stored procedures

Use `StoredProcedureBase<TParameters>` for commands without rows and the matching `ResultStoredProcedureBase<...>` for returned result sets. Select the SQL Server or PostgreSQL package explicitly.

- Register the generated parameter mapper with `SqlParameterMapperFactory` or `NpgsqlParameterMapperFactory`; register required data-reader/result mappers too.
- Call the routine with the repository's protected `GetDbConnection()` so it uses the context-owned connection. Do not dispose that connection.
- Pass the scoped Unit of Work when the command must enlist in its active transaction. Resolve every participating repository before opening that transaction.
- SQL Server tuple positions follow result-set order. PostgreSQL multi-result tuple positions follow implementation-defined distinct cursor-name enumeration and do not promise database return order; avoid assigning different business meaning by tuple position unless the application identifies it explicitly.
- Set a reviewed command timeout, preserve cancellation where the installed API supports it, and document provider-specific connection behavior.
- Review generated routine signatures after schema changes; never hand-edit generated mappers.
- When a routine result type changes, update its declared result type and rerun the repository's atomic
  mapper generator. Remove obsolete result models, mappers, and registrations in the same change. Do not
  add a parallel handwritten mapper or registry for a generator-owned boundary. Run generation twice and
  compare the complete generated path/content set to prove determinism and checked-in output freshness.
- Materialize a paged stored-procedure result in one database command. Do not page identifiers and hydrate
  their view rows in a second query: concurrent changes can split count/order/filtering from the returned
  projection or make an identifier disappear between reads.

## Context and Unit of Work

Repository construction registers its context with the scoped Unit of Work. Resolve every participating repository before creating an explicit transaction.

## SQL Server table-valued parameters

Typed Paradigm SQL Server procedure callers support table-valued parameters. Keep the procedure wrapper, its input contract, and mapper registration in Data; a table input is not a reason to abandon the wrapper or split out a `Data.SqlServer` application project.

The reviewed mapper generator accepts an `IEnumerable<TRow>` property with supported scalar row properties, or a supported property-based single-row object, and builds a `DataTable`. It does not infer every collection/composite shape: check the installed generator before substituting `List<TRow>`, arrays, scalar collections, or nested objects. A generator diagnostic for one unsupported shape is not evidence that the caller API cannot accept TVPs.

Match the SQL user-defined table type's column order, CLR/SQL types, and nullability. Generated row columns are ordered by property name, so verify their ordinal order against the database type. Send an empty table with the proper columns for zero rows; do not replace a table value with `DBNull.Value`. Test populated, empty, nullable-column, and unsupported-shape cases.

When explicit `SqlDbType.Structured` or `SqlParameter.TypeName` metadata is required, use a reviewed application-owned `ISqlParameterMapper` registered through `SqlParameterMapperFactory` for that input contract, or change the owning generator deliberately. Its `Map` returns `SqlParameter[]`, so it can supply a parameter with the schema-qualified database type name and a `DataTable` value. `SqlParameterMapperBase` currently exposes the name/value helper, not a constructed-parameter overload; do not invent such an overload. Do not patch generator-owned output. Factory registration is first-wins: register an intended custom factory before bulk generated registration and verify which mapper resolves. Unsupported shapes still need a reviewed generation/ownership decision; there is no implied per-type exclusion switch. Returned parameters and their row sources must survive mapper disposal because the caller disposes the mapper before execution.

See [Microsoft's SqlClient parameter guidance](https://learn.microsoft.com/en-us/sql/connect/ado-net/configure-parameters) for the structured parameter contract. Verify mapper construction without a database, then test actual procedure execution against a disposable SQL Server database before claiming the database type/ordinal contract works end to end.
