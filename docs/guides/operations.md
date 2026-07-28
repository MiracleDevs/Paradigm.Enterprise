# Operations and health

Operational behavior belongs to the host because deployment environments differ. Paradigm.Enterprise supplies health checks for cache and blob storage and integrates with standard ASP.NET Core health checks.

## Health checks

Register checks only for dependencies that affect the host profile. A public read-only host and an administrative host may have different critical dependencies.

Map unhealthy results to an appropriate non-success status such as 503. Keep public health output minimal; exception text and connection details belong in protected logs.

## Logging and telemetry

Use structured logging at request and use-case boundaries. Include identifiers that help correlate work without logging credentials, tokens, message bodies, file content, or personal data.

Providers are a useful place to measure application use cases. Repositories are useful for query timing. Middleware can measure HTTP behavior. Keep telemetry dependencies out of domain entities.

## Degraded infrastructure

The cache package can fall back to a null implementation when configured not to throw on recognized startup failures. This keeps the process available but changes performance and cache semantics. Emit a health signal and alert when the fallback is active.

Email failures are logged and swallowed by the current service. If delivery is operationally important, monitor failures and move guaranteed delivery into a durable workflow.

## Cancellation and shutdown

Propagate cancellation through application code where library APIs accept it. Dispose readers, streams, transactions, and scoped services predictably. Avoid starting background work from a request without an owned queue or hosted service.
