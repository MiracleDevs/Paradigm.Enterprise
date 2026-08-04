# Beacon AR CLI Repository SQL Policy - Change Summary

## Delivered

- Added compiled Roslyn diagnostic `PE3107` to `paradigm checks run`. It analyzes production source classes semantically assignable to `Paradigm.Enterprise.Domain.Repositories.IRepository`, including every partial declaration and the complete post-generator compilation tree set.
- `PE3107` reports EF `FromSql*`, `ExecuteSql*`, and `SqlQuery*` calls; `DbConnection`/`IDbConnection` command creation; `DbCommand`/`IDbCommand` text assignment and execution; resolved Dapper-family and equivalent SQL-bearing connection extensions, including conditional access; raw command construction; and repository-owned SQL-bearing fields, properties, constants, methods, local functions, and assignments after declaration.
- The SQL-statement recognizer for repository-owned declarations and assignments is deliberately conservative and uses constant/interpolated semantic values plus statement structure. Resolved EF/ADO/Dapper/equivalent connection-extension sinks do not depend on argument foldability. The policy does not classify prose, log messages, routine object names, ordinary filter values, or an arbitrary business `QueryAsync`/`ExecuteAsync` by name.
- Allowed boundaries remain EF/LINQ CRUD and simple bounded queries, calls to typed SQL Server or PostgreSQL stored-procedure wrappers, infrastructure outside repositories, database/bootstrap/generator source, and projects explicitly evaluated with `IsTestProject=true`.
- Added the evaluated `IsTestProject` property to the compiled-check project model. Semantic compilation still fails closed before any rule runs; there is no syntax/name fallback and no fixer.
- Preserved stable diagnostic ordering and duplicate elimination. The message includes the fully qualified repository member and the source location remains the exact evaluated path, line, and column.
- Changed `checks run` so an expired suppression reports `PE7004` but does not prevent semantic checks from running. The expired suppression therefore no longer hides the underlying `PE3107`; unrelated invalid configuration still blocks execution.

## Deterministic fixtures

- The focused semantic fixture produces exactly 41 unique `PE3107` errors. An explicit ordered manifest asserts every `(code, severity, fully-qualified member, construct, file, line, column)` tuple, including a stable location from a real incremental source generator.
- Coverage includes raw/interpolated EF families, `DbConnection` and `IDbConnection`, command initialization/text/execution, Dapper-family calls, equivalent connection extensions, conditional-access receivers, raw and folded SQL declarations, post-declaration field/property assignments, a local function, partial repositories, checked-in generated source, and emitted generated source.
- Runtime-value coverage proves that direct `DbConnection` string commands and conditional `IDbConnection` `FormattableString` commands remain prohibited without foldable SQL. False-positive coverage proves no finding for typed SQL Server and PostgreSQL wrappers, EF CRUD/LINQ, business `QueryAsync`/`ExecuteAsync`, a non-database receiver with a SQL-looking string, a connection extension whose non-string parameter is named `sql`, procedure object names, non-repository infrastructure SQL, and a repository test double in an `IsTestProject=true` project.
- Repeated analysis returns byte-for-byte equal ordered diagnostic records with no duplicate code/location/message tuple.
- The incomplete-compilation fixture fails closed on its unresolved type before `PE3107` can run.
- The CLI-level fixtures use fixed dates and exact symbols plus locations. An active suppression removes only one of 41 findings; expired PE3107 and unrelated PE3103 suppressions each emit `PE7004`, execute the built-in C# check, and restore the underlying diagnostic. An unrelated malformed configuration emits `PE5001`, preserves exit code 1, and leaves built-in checks unexecuted.
- The real generator fixture adds no `PackageReference`, package identity, version, or lock-file entry. Its `netstandard2.0` compiler reference reuses the centrally governed Microsoft.CodeAnalysis 5.0.0 asset restored through the already-approved `Paradigm.Enterprise.Checks.CSharp` project edge.

## Diagnostic-count reconciliation

- The first review evidence recorded 24 findings. The completed pre-correction fixture reached 29 by adding five previously absent exact cases: `Microsoft.Data.SqlClient.SqlCommand.ctor`, the static `DapperLikeExtensions.ExecuteAsync` call, the `IDbConnection`-based `EquivalentSqlExtensions.QueryContractAsync` call, checked-in generated `GeneratedRawSqlRepository.Statement`, and partial `RawSqlRepository.PartialSql`.
- The final review corrections intentionally raise 29 to 39: four conditional-access sinks (`DbConnection.CreateCommand`, `DbCommand.ExecuteReader`, Dapper `QueryAsync`, and equivalent `ExecuteStatementAsync`), four repository-owned SQL assignments, the directly resolved `BuildSqlLiteral` helper declaration, and one repository declaration emitted by a real incremental source generator. The exact manifest prevents a missing tuple from being concealed by an unrelated extra result.
- Cycle-2 corrections raise 39 to the final 41 by adding two exact runtime-value sinks: direct `DbConnection.ExecuteStatementAsync(string)` and conditional `IDbConnection.QueryContractInterpolatedAsync(FormattableString)`. No existing tuple was removed; the unsafe prose-on-connection negative call was removed because a resolved raw-command sink is prohibited independently of its current argument contents.
- The packed CLI integration boundary asserts that the installed tool reports `PE3107`; no parallel tool, runtime check pack, or fixer was added.

## Guidance and documentation

- Updated the CLI catalog and suppression example, CLI maintainer guidance, changelog, good coding practices, database practices, and the repository/database/review/evolve skills.
- Durable guidance now states that production repositories use EF/LINQ for simple bounded work and typed database-project routines for complex, paginated, locking, multi-entity, or performance-sensitive work, with `PE3107` as the deterministic ownership gate.

## Validation evidence

- Focused `CSharpCheckServiceTests`: 11/11 passed after the final review corrections.
- Framework Release restore/build: passed with 0 warnings and 0 errors.
- Framework non-integration tests: 126/126 framework tests and 86/86 CLI tests passed.
- Packed artifacts/readmes verified; the packed CLI installed successfully; all 20/20 integration tests passed, including the installed-tool `PE3107` assertion and the final suppression/configuration regressions.
- Installed-tool framework `packages check` passed. `packages audit` completed without vulnerabilities or errors and reported only existing available-update `PE7008` warnings. Installed semantic checks passed for the CLI, CLI tests, C# checks, and code generator projects.
- Beacon AR Release restore/build passed with 0 warnings and 0 errors. The Microsoft Testing Platform solution run discovered 203 tests: 161 passed, 42 live-database tests skipped because no connection string was supplied, and 0 failed.
- Beacon SQL Server project build passed with 0 warnings/errors; repository-source `database validate --strict` passed. Final whole-solution `doctor` and `validate` passed with zero diagnostics after the final Release build.
- Beacon project-scoped semantic checks for Domain, Data, Providers, and WebApi all passed with zero diagnostics and zero `PE3107`. The whole-solution check has zero `PE3107` but retains the established `PE1002` limitation because the standalone Roslyn compilation cannot resolve Aspire AppHost's generated `Projects` namespace.
- OpenAPI generation was run twice and stayed byte-stable at `F0398F38EDA2976BC7937FCD673D70E1F40C17BEF92A7B02C3FE96223EE1B3EB`.
- TypeScript client generation was run twice and stayed byte-stable at `DA1FDFC3F9B83F19D55A9607A578AD4CF3F93737FF6AD16102B64D62777840F0`; `npm ci` found no vulnerabilities and strict `tsc --noEmit` passed. Node 24.13 remains below Angular's requested 24.15 patch range and produced only the established engine warning.
- Stored-procedure mapper generation covered 36 generated files, was byte-stable across the final repeated run, and left no staging directories. No generated persistence file, mapper, OpenAPI artifact, client, or T4 template remains changed.
- Skill validation passed for all 11 Paradigm skills, plugin metadata validation passed at 1.1.0, Docfx passed with warnings as errors, `git diff --check` passed, and supplemental repository-file scans found zero raw-SQL patterns in Beacon AR, the official template, and RDX DMS.

## Baseline boundaries and environment gaps

- RDX's canonical solution completed semantic analysis with exactly zero `PE3107`. It also reports 4,215 pre-existing `PE3104`/`PE3105`/`PE3106` findings unrelated to this rule.
- The official template's canonical source contains unexpanded `$ext_safeprojectname$` tokens and cannot form a semantic compilation. A disposable rendered template was also blocked by existing template/current-package drift: `Microsoft.Extensions.Configuration.Json` 10.0.1 versus framework 10.0.9 (`NU1605`) and the obsolete non-generic `DbContextBase` use. Both attempts produced zero `PE3107` before `PE1002`; the supplemental repository scan found no raw-SQL patterns. The exact disposable template directory was removed.
- The checked-in `build/quality.sh` has CRLF endings and Bash rejected `set -o pipefail`. Its material stages were reproduced explicitly: clean restore/build/non-integration tests, pack/readme verification, packed-tool installation/integration tests, package policy/audit, installed checks, skill/plugin validation, and Docfx.
- A fresh live SQL Server/API gate was not created for this CLI-only semantic-analysis change. Task 5 already recorded a fresh 24/24 database and 60/60 API gate; this task changed no Beacon application, database, generated persistence, mapper, OpenAPI, or client source. No Docker container, image, network, or volume was created or removed, and the pre-existing Aspire resources were untouched.
