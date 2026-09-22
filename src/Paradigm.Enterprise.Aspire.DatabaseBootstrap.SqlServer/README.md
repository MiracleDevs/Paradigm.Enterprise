# Paradigm.Enterprise.Aspire.DatabaseBootstrap.SqlServer

Registers an application-owned, finite SQL Server bootstrap Dockerfile with a database dependency, secret password parameter, bounded publish switch, and completion-based consumer ordering.

The consuming application owns its Dockerfile, bootstrap script, DACPAC, optional BACPAC, scripts, schema probe, and database publication implementation.
