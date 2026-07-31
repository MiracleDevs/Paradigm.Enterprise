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

## Context and Unit of Work

Repository construction registers its context with the scoped Unit of Work. Resolve every participating repository before creating an explicit transaction.
