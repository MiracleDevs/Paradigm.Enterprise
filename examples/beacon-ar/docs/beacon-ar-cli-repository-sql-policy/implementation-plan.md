# Beacon AR CLI Repository SQL Policy — Implementation Plan

## Outcome

Add a compiled Roslyn check, `PE3107`, to `paradigm checks run` so production repository implementations cannot contain handwritten SQL or raw database-command plumbing. Keep `paradigm validate` metadata-only. Preserve the reviewed boundary already used by Beacon AR, the Paradigm Web API template, and the RDX DMS reference application: simple bounded CRUD/query work uses EF/LINQ; complex, paginated, locking, multi-entity, or performance-sensitive work calls a typed stored-procedure wrapper whose SQL object lives in the database project.

## Current evidence and integration points

- `src/Paradigm.Enterprise.Checks.CSharp/Analysis/CSharpChecks.cs` already constructs the real project compilation, runs source generators, rejects an incomplete semantic compilation, evaluates each handwritten syntax tree with a `SemanticModel`, and sorts diagnostics deterministically.
- `src/Paradigm.Enterprise.Checks.CSharp/Analysis/EvaluatedProject.cs` obtains the project's `Compile`, `ReferencePath`, and `Analyzer` items and compilation properties through MSBuild. Extend its evaluated properties only as needed to carry `IsTestProject` into analysis.
- `src/Paradigm.Enterprise.Cli/Commands/Checks/ChecksRunCommandHandler.cs` already converts compiled-check diagnostics, applies `.paradigm/config.json` suppressions, reports expired suppressions through `PE7004`, and exposes the built-in `PE31` family. No new route or configuration schema is needed.
- `src/Paradigm.Enterprise.Cli.Tests/CSharpCheckServiceTests.cs` and `src/Paradigm.Enterprise.Cli.Tests/Fixtures/SemanticViolations` are the focused semantic-check harness. `PackedCliIntegrationTests.cs` is the installed-tool boundary.
- Reviewed source searches found typed stored-procedure wrapper calls, and no handwritten SQL in repository implementations, in Beacon AR, the Paradigm Web API template, and `C:/Repositories/github/microsoft/rdx-dms-mvp/src/api`. These are positive regression targets, not exceptions encoded by application name or path.

## Implementation sequence

1. Add a focused repository-SQL analysis pass to `CSharpChecks.Analyze` after the compilation-error gate. Keep it in `src/Paradigm.Enterprise.Checks.CSharp/Analysis`; split it into a narrowly named file/type if that is required to preserve one semantic type per file and keep `CSharpChecks` readable. Reuse the compilation and semantic models; do not parse source independently, use reflection, load an extension, or add a package.

2. Define repository scope semantically. Analyze a source-declared class only when its type hierarchy is assignable to `Paradigm.Enterprise.Domain.Repositories.IRepository`; include every partial declaration/member of that type. Do not infer scope from a `Repository` suffix, a `Repositories` folder, or an application-specific namespace. Skip projects for which the evaluated MSBuild `IsTestProject` property is true. Do not add a blanket generated-code exemption: a generated production repository requires the same safe boundary or a reviewed, expiring suppression.

3. Report `PE3107` with severity `error` for these constructs inside an in-scope repository:

   - EF relational raw-SQL calls resolved to the Microsoft EF symbols: `FromSql*`, `ExecuteSql*`, and `Database.SqlQuery*`, including synchronous, asynchronous, raw, and interpolated variants.
   - Creation or direct use of ADO commands: `DbConnection`/`IDbConnection.CreateCommand`, assignment or object-initializer assignment to `DbCommand`/`IDbCommand.CommandText`, and execution APIs on a command (`ExecuteReader*`, `ExecuteScalar*`, or `ExecuteNonQuery*`).
   - Dapper and equivalent SQL-bearing connection extension calls when the resolved method is in the Dapper family or is an extension over `DbConnection`/`IDbConnection` with a SQL/command-text parameter, including `Query*`, `Execute*`, `ExecuteReader*`, and `ExecuteScalar*`. Do not flag an arbitrary business method merely because it is named `ExecuteAsync`.
   - Repository-owned constants, fields, properties, local functions, or methods that hold/return a compile-time, interpolated, or raw-string SQL statement. Use a conservative SQL-statement recognizer over constant/foldable content (statement verb plus corroborating SQL structure), so prose, log text, enum codes, routine object names, and ordinary filter values do not match. Report the declaration as well as a raw sink only once per stable code/location/message tuple.

4. Preserve explicit allowed cases:

   - calls on types derived from the SQL Server or PostgreSQL `StoredProcedureBase`/`ResultStoredProcedureBase` hierarchy, including their `ExecuteAsync` methods and typed routine-name overrides outside repository types;
   - EF `DbSet` CRUD, change tracking, `SaveChanges`, and LINQ expressions/materialization that do not invoke raw SQL;
   - SQL database-project files, deployment/bootstrap executables, code generators, and test projects, because they are outside the production `IRepository` compilation/type scope;
   - non-SQL string constants and ordinary `ExecuteAsync` collaborators.

5. Make the diagnostic contract stable. Use this message shape:

   ```text
   {fully-qualified repository member} uses handwritten SQL or raw database command '{resolved API or construct}'. Use EF/LINQ for simple bounded work or a typed stored-procedure wrapper whose SQL lives in the database project.
   ```

   Locate the diagnostic at the offending invocation, assignment, initializer, or SQL-bearing declaration as `absolute-or-evaluated-source-path(line,column)`. Include the fully qualified repository member in the message so existing symbol suppressions are exact. Keep final ordering by code, location, then message and exact duplicate elimination. Do not add a fixer: the correct replacement requires database design, result/parameter contracts, generation, transaction, and performance judgment.

6. Expand focused fixtures without placing invalid source in product projects:

   - Add repository violations covering every EF family, interpolated SQL, ADO `CreateCommand`/`CommandText`/execution, Dapper-like connection extensions, and SQL constants/fields/helper methods.
   - Add false-positive/allowed fixtures for typed SQL Server and PostgreSQL wrappers, EF CRUD/LINQ, a business `ExecuteAsync`, stored-procedure object-name strings, non-SQL prose, a similarly named non-repository type, and a real test project with an `IRepository` test double containing SQL.
   - Keep the fixture compilation complete. Add/retain a `SemanticIncomplete` assertion proving analysis fails closed before rules run when symbols cannot be resolved; never fall back to method-name-only detection.
   - In `CSharpCheckServiceTests.cs`, assert exact `PE3107` count, severity, message symbol, location, allowed cases, duplicate elimination, and identical ordering over repeated runs.
   - Add a CLI-level test through `TestCliApplication.RunAsync` (and retain the packed-tool check in `PackedCliIntegrationTests.cs`) proving an unexpired symbol/location suppression removes only the matching `PE3107`, while an expired suppression produces `PE7004` and leaves `PE3107` active. Use fixed dates, not `UtcNow` arithmetic, wherever expiry behavior is asserted.

7. Run the rule against the three established application baselines and treat every `PE3107` as a defect unless evidence proves a reviewed suppression is necessary:

   - `examples/beacon-ar/BeaconAr.slnx`
   - the current Paradigm Web API template solution available locally
   - `C:/Repositories/github/microsoft/rdx-dms-mvp/src/api/Microsoft.DemoManagementSystem.slnx`

   Do not encode these names or paths in the rule. If the template/reference solution uses `.sln`, use its canonical existing solution rather than adding another solution file.

8. Update durable guidance and discoverability:

   - `docs/cli.md`: add `PE3107`, its error severity/boundary, suppression example, allowed typed-wrapper/EF cases, and extend the `PE31` diagnostic range.
   - `docs/contributing/cli-development.md`: state that repository SQL policy belongs to complete semantic `checks run`, explain the fail-closed compilation and fixture boundary, and retain the trust warning for MSBuild/generator execution.
   - `CHANGELOG.md`: record the new deterministic repository SQL policy under the current release.
   - `.agents/references/good-coding-practices.md`, `.agents/skills/paradigm-build-repository/SKILL.md`, `.agents/skills/paradigm-review-change/SKILL.md`, and `.agents/skills/paradigm-evolve-guidance/SKILL.md`: keep the concise durable rule and link/refer to the built-in `PE3107` enforcement without duplicating implementation details. Update deterministic skill-validation expectations if those files are asserted.
   - Write `change-summary.md` after implementation and maintain `review-feedback.md` through the required review/fix cycles.

## Validation and acceptance

Run in this order, recording exact results and environment gaps in `change-summary.md`:

1. Focused CLI/check tests, including the semantic fixtures and packed-tool boundary.
2. `dotnet restore`, `dotnet build`, and `dotnet test` for `src/Paradigm.Enterprise.slnx`.
3. `dotnet tool run paradigm doctor`, `packages check`, `packages audit`, `validate`, and `checks run` for the framework and Beacon AR canonical solutions; run `database validate --strict` for Beacon's SQL Server project.
4. Beacon AR architecture/domain/provider/API tests, deterministic generation/OpenAPI/client checks, and the established live SQL Server/API validation path.
5. The canonical template and RDX application `checks run` baselines, subject to their existing restore/environment prerequisites.
6. Skill validation, plugin validation, Docfx with warnings as errors, `git diff --check`, and a final search confirming production repositories contain neither raw-SQL APIs nor SQL-shaped query/command text.
7. Track every Docker container, image, volume, network, and temporary directory created specifically for this task and remove only those task-owned resources after live validation. Verify the exact resource names before cleanup and leave pre-existing Aspire resources untouched.

Acceptance requires deterministic `PE3107` findings for all prohibited fixtures, no false positives in the allowed fixtures or three application baselines, complete semantic compilation, working exact suppressions with expiry enforcement, no fixer, clean documentation/skill validation, and no task-owned Docker residue.
