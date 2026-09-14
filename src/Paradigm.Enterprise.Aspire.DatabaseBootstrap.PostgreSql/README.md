# Paradigm.Enterprise.Aspire.DatabaseBootstrap.PostgreSql

Registers an application-owned, finite PostgreSQL bootstrap Dockerfile with a database dependency and the explicit `Paradigm_ORM_ConnectionString` expected by DbPublisher-based bootstraps.

The consuming application owns its Dockerfile, bootstrap script, DbPublisher version, schema inputs, and schema probe.
