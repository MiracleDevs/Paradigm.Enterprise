# Repository patterns

## Aggregate deletion

Expose removal through the aggregate repository. Use the protected aggregate-removal helpers supplied by the selected base. Do not give callers a child repository solely to bypass the root.

## Search

Implement the protected paginated search function or a focused custom method. Apply a stable order before paging. Treat search parameters as application/data contracts, not HTTP request objects.

## Stored procedures

Keep connection ownership, command execution, result mapping, nullability, parameter direction, timeouts, and cancellation in Data. Review generated routine signatures after schema changes; do not edit generated mappers.

## Context and Unit of Work

Repository construction registers its context with the scoped Unit of Work. Resolve every participating repository before creating an explicit transaction.
