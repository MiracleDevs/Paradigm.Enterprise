# Operations and health

Operational behavior belongs to the host because deployment environments differ. Paradigm.Enterprise supplies health checks for cache and blob storage and integrates with standard ASP.NET Core health checks. It does not configure a telemetry backend, alert policy, service objective, retry strategy, backup, or recovery plan.

## Health checks

Register checks only for dependencies that affect the host profile. A public read-only host and an administrative host may have different critical dependencies.

Map unhealthy results to an appropriate non-success status such as 503. Keep public health output minimal; exception text and connection details belong in protected logs.

## Logging and telemetry

Use structured logging at request and use-case boundaries. Prefer stable message templates and named properties so logs can be searched and aggregated. Include identifiers that help correlate work without logging credentials, tokens, message bodies, file content, or personal data.

Providers are a useful place to measure application use cases. Repositories are useful for query timing. Middleware can measure HTTP behavior. Keep telemetry dependencies out of domain entities.

Logs explain discrete events, metrics show behavior over time, and traces connect work across boundaries. Propagate a correlation or trace identifier through asynchronous and remote work where the platform supports it. Do not invent a second unrelated identifier when the host already provides a trace context.

Request rate, failures, and duration are useful starting signals for an API. Resource utilization and saturation can explain dependency or capacity problems. Define an SLI or SLO only when the service has an owner, a reliable measurement source, and an agreed response when the objective is missed. The library does not define these targets.

Alerts should represent conditions that require action. Give each alert a severity, owner, useful context, and runbook. Avoid alerting on every exception or exposing detailed diagnostics through a public health response.

## Degraded infrastructure

The cache package can fall back to a null implementation when configured not to throw on recognized startup failures. This keeps the process available but changes performance and cache semantics. Emit a health signal and alert when the fallback is active.

Email failures are logged and swallowed by the current service. If delivery is operationally important, monitor failures and move guaranteed delivery into a durable workflow.

Timeouts should bound external work. A bounded retry may help with a transient failure when the operation is safe to repeat. Circuit breakers, bulkheads, and rate limits can isolate failure or protect capacity. These policies belong to the host or an infrastructure adapter and must reflect the operation's semantics. The Enterprise service packages do not add them automatically.

An outbox or durable queue can help when a database change must lead to reliable external work. That design needs duplicate handling, monitoring, repair, and retention. A normal Unit of Work commit cannot provide those guarantees.

## Cancellation and shutdown

Propagate cancellation through application code where library APIs accept it. Dispose readers, streams, transactions, and scoped services predictably. Avoid starting background work from a request without an owned queue or hosted service.

Readiness should become unhealthy before shutdown stops accepting work, while in-flight requests and owned background operations receive time to complete or cancel. The exact sequence is a host and deployment-platform decision.

## Recovery and operational readiness

Backups, restoration, regional resilience, and disaster recovery are platform responsibilities. Define acceptable data loss and recovery time from the importance of the service, then test restoration instead of assuming that a successful backup job proves recoverability.

Before a material release, verify that the deployed version is identifiable, required dependencies are observable, alerts lead to an owner, and rollback or recovery steps match the database and external side effects involved. [Secure delivery](secure-delivery.md) covers the complete release path.
