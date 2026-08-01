# Changelog

All notable changes to this project will be documented in this file.

Version `1.1.0`

- Added installable Agent Skills for guided application design, implementation, security, and review.
- Added focused Aspire orchestration and SQL Server/PostgreSQL database-engineering skills, canonical database practices, a safe template scaffolder, and deterministic database-project validation.
- Consolidated diagnostics, semantic C# checks, and JSON/mapper/OpenAPI generation behind one installable `Paradigm.Enterprise.Cli` tool; analyzer and generator projects are internal package components rather than separate tools.
- Added built-in `PE3103`/`PE3104` semantic checks plus deterministic `PE3105` one-type-per-file and `PE3106` member-region, order, and spacing checks with generated-code exclusions.
- Reorganized the single CLI by capability, protected its `Packages` source folder from NuGet ignore rules, and added complete user and maintainer documentation for commands, options, architecture, packaging, and verification.
- Added `build/quality.sh` as a repeatable local Bash entry point for the complete PR quality workflow.
- Removed unreleased CLI/framework lockstep, check-pack versions, executable configuration, minimum-version branches, and legacy API-shape guidance while retaining ordinary NuGet release metadata and installed-package inspection.
- Split generator assembly input from source output, exposed independent generation modes, and made generation failures return nonzero `PE8001` results instead of being swallowed.
- Added canonical Paradigm Good Coding Practices covering member order/regions, semantic file layout, domain folders/contexts, least visibility, immutability, browser security, dependency approval/licensing, and API observability/health.
- Strengthened every Paradigm skill and deterministic skill validation to reference the canonical practices.
- Updated ExampleApp to .NET 10, current generic identifier APIs, behavior-owned entity transitions, and aligned Paradigm 1.1.0 packages.
- Added PR and scheduled dependency workflows, Copilot review guidance, centralized release-version validation, expanded CLI/tool fixtures, and warning-as-error documentation validation.

Version `1.0.33`

- Added fix in `Services.Cache` package crashing when Redis connection failed.
- Added the read-only `Paradigm.Enterprise.Cli` .NET tool for restored-package API discovery and application diagnostics.

Version `1.0.32`

- Added retry logic to Upload and Download operations in `Services.BlobStorage` package.
- Updated dependencies.

Version `1.0.31`

- Upgraded EF Core and related data-access packages to `10.x` (including `Microsoft.EntityFrameworkCore*`, `EntityFrameworkCore.Exceptions.SqlServer`, and `Npgsql.EntityFrameworkCore.PostgreSQL`).
- Upgraded additional dependencies (e.g., `Azure.Identity`, `Azure.Storage.Blobs`, `Microsoft.Data.SqlClient`, and Redis packages).
- Updated some projects that previously multi-targeted `net9.0;net10.0` to target `net10.0` only.

Version `1.0.30`

- Added support for managed identity to `Services.Cache` and `Services.Email` packages.

Version `1.0.29`

- Added support for JSON in `TableReaderService`

Version `1.0.28`

- Support `int` and `Guid` as Entity id type.

Version `1.0.26`

- Added `Reset()` method to `DomainTracker<TEntity>` class.

Version `1.0.25`

- Added support for net10.

Version `1.0.24`

- Fixed `ExceptionHandler` class to return the original Exception type.

Version `1.0.23`

- Fixed `CacheService` registration method.

Version `1.0.22`

- Added `TableWriter` service that provides a unified service for exporting table data into multiple file formats including Excel (.xlsx), CSV, and XML.
- Removed JSON support from `TableReader` service. **BREAKING CHANGE**: key enumeration was renamed and must be replaced when upgrading the library version.

Version `1.0.21`

- Added protected `RemoveAggregate` methods to `EditRepositoryBase` to allow aggregate root repositories to delete aggregated child entities without requiring separate repositories, maintaining DDD aggregate boundaries.
- Changed `DeleteRemovedAggregatesAsync` to synchronous `DeleteRemovedAggregates` method, as the underlying operations are synchronous. **BREAKING CHANGE**: Any existing repository implementations overriding `DeleteRemovedAggregatesAsync` will need to update to the new synchronous signature.

Version `1.0.20`

- Fixed `AzureBlobStorageHealthCheck` to support managed identity. Updated dependencies.

Version `1.0.19`

- Fixed `Cache` package extension methods.

Version `1.0.18`

- Added support for NET8 to `Data.SqlServer` and `Domain` libraries.

Version `1.0.17`

- Modified `TableReader` service to support stream-based reading for various formats and implemented multiple enhancements.

Version `1.0.16`

- Added `TableReader` service package that provides a unified service for reading and parsing multiple file formats including Excel (.xlsx, .xls), CSV, JSON, and XML.

Version `1.0.15`

- Modified `BlobStorageService` to support Azure Managed Identity authentication. Added Health Checks to the `BlobStorage` and `Cache` packages.

Version `1.0.14`

- Search functionality refactor.

Version `1.0.13`

- Added MIT license information

Version `1.0.12`

- Moved entity audit logic to `Data` layer.

Version `1.0.11`

- Modified `Data` to allow to set the command timeout when executing a stored procedure.

Version `1.0.10`

- Adjusted GetSearchPaginatedFunction in ReadRepositoryBase.

Version `1.0.9`

- Modified `Data.PostgreSql` to automatically create a transaction when execute a stored procedure.

Version `1.0.8`

- Added new methods in `EditProviderBase` to execute on save before the view is mapped to an entity.

Version `1.0.7`

- Added GetArray method to read values from database array fields.

Version `1.0.6`

- `IAuditableEntity` interface refactor to support `DateTime` or `DateTimeOffset` types.

Version `1.0.5`

- Removed `ZiggyCreatures.FusionCache` library from `Services.Cache`.

Version `1.0.4`

- Fixed issue in `Services.Cache` package. Included packages debug symbols.

Version `1.0.3`

- Fixed DataReaderMapperFactory bug in `Data` package. Added new methods in `Services.Cache` package.

Version `1.0.2`

- Fixed ParameterMapperFactory bug in `Data.SqlServer` and `Data.PostgreSql` packages.

Version `1.0.1`

- Implemented `ZiggyCreatures.FusionCache` library in `Services.Cache`. Implemented `Data.PostgreSql` package.

Version `1.0.0`

- Uploaded first version of the Paradigm.Enterprise.
