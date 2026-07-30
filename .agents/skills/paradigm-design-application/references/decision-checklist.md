# Architecture decision checklist

For each important decision, record context, chosen option, consequences, rejected alternatives, and a reconsideration trigger.

Confirm:

- each capability has a vocabulary, owner, public contract, and data owner;
- every invariant has one enforcement owner;
- every multi-step write has a named transaction boundary;
- every external side effect has timeout, duplicate, partial-failure, and recovery behavior;
- every route has an intentional exposure and authorization policy;
- telemetry identifies a request/use case without secrets or unnecessary personal data;
- readiness reflects critical dependencies and has an operator response;
- public contracts and stored data have an upgrade/rollback story.
- deployment defines environment configuration and secrets, schema-migration ownership/order, startup and shutdown behavior, health probes, capacity/timeouts, rollback compatibility, and post-deploy verification.
