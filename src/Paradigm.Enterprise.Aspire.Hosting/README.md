# Paradigm.Enterprise.Aspire.Hosting

Reusable local .NET Aspire composition helpers for Paradigm applications. The package loads a repository-root `.env` file without overwriting process values and provides managed or external SQL Server, PostgreSQL, Azure Storage emulator, Redis, and existing JavaScript-client recipes.

`AddParadigmJavaScriptClient` accepts an existing JavaScript client directory, its package run script, fixed or dynamic port options, one or more API references, and optional API-start waiting. It works with frameworks such as Angular and React, but does not generate client code or own proxy rules.

Application AppHosts retain project references, schema assets, ports, API settings, client proxy rules, and deployment policy. The package is intended for local orchestration and integration testing.
