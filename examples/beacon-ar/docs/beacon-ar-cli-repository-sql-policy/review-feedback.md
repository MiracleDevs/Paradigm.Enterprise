# Beacon AR CLI repository SQL policy - review feedback

> Historical review record: the 24- and 29-finding counts describe cycle-1 interim states, and the 39-finding count describes the cycle-2 pre-correction state. The corrected implementation's current contract is the exact ordered 41-tuple manifest recorded in `change-summary.md`; this record is retained to preserve the review trail until re-review.

## Review status

**Not approved.** The current implementation builds and its checked fixture reports 29 deterministic `PE3107` errors, but the semantic policy still has enforceable coverage holes. No Task 6 implementation code was changed by this review.

## Findings

### P1 - Source-generator output is excluded from PE3107

Paths:

- `src/Paradigm.Enterprise.Checks.CSharp/Analysis/CSharpChecks.cs:32-38,52-55`
- `src/Paradigm.Enterprise.Cli.Tests/Fixtures/SemanticViolations/GeneratedRawSqlRepository.g.cs:1-8`
- `src/Paradigm.Enterprise.Cli.Tests/CSharpCheckServiceTests.cs:90-91`

The generator driver updates `compilation`, but `RepositorySqlPolicy.Analyze` receives the original `trees` array created from evaluated `Compile` items. A repository emitted by an `ISourceGenerator`/`IIncrementalGenerator` is present in `compilation.SyntaxTrees` but absent from that array, so raw SQL in it is never visited. The current `.g.cs` fixture is a checked-in `Compile` item and proves only filename-based generated source, not generator output. This contradicts the documented claim that every generated production repository is covered.

Smallest safe correction: analyze the complete post-generator compilation tree set for PE3107 while preserving the existing generated exclusions for unrelated layout rules. Add a real source generator fixture that emits an `IRepository` with one raw sink and assert its exact PE3107 tuple and stable generated-tree location.

### P1 - Resolved ADO/equivalent-command calls can bypass receiver and parameter classification

Path: `src/Paradigm.Enterprise.Checks.CSharp/Analysis/RepositorySqlPolicy.cs:137-176,183-198`

`ReceiverType` recognizes only an explicit `MemberAccessExpressionSyntax` or the first argument of an unreduced static extension call. A resolved conditional-access call such as `connection?.CreateCommand()`, `command?.ExecuteReader()`, or `connection?.QueryAsync(...)` uses a member-binding expression, produces no receiver here, and is missed. Equivalent connection extensions are additionally classified by parameter *name* (`sql` or `commandText`) rather than a semantic SQL-bearing parameter/argument contract. `ExecuteAsync(this DbConnection, string statement)` is missed, while a non-string parameter merely named `sql` can be reported. These are production-policy bypasses and name-based false-positive/negative behavior.

Smallest safe correction: recover the receiver from the enclosing conditional-access expression and classify equivalent extensions from resolved receiver plus SQL-bearing parameter type/argument association, with Dapper's resolved family handled explicitly. Add exact fixtures for conditional ADO create/execute, conditional Dapper/equivalent calls, a string parameter named `statement`/`query`, and a non-string parameter named `sql` that must remain clean.

### P1 - Repository-owned SQL assigned after declaration is not analyzed

Path: `src/Paradigm.Enterprise.Checks.CSharp/Analysis/RepositorySqlPolicy.cs:86-112,238-275`

Field/local declaration initializers and direct property/method/local-function returned expressions are checked, but an assignment that stores SQL into a repository field or property is ignored unless the target is `CommandText`. For example, a constructor assignment `_statement = "SELECT ... FROM ..."` or `Statement = BuildSqlLiteral` leaves repository-owned handwritten SQL undiagnosed. This misses the required repository-owned declaration/helper flow even without interprocedural taint analysis.

Smallest safe correction: inspect assignments whose resolved target is a field/property owned by the repository and whose right side is a recognized SQL expression. Keep the check local and constant/foldable; do not introduce general taint analysis. Add exact field/property constructor-assignment fixtures and non-SQL assignment controls.

### P2 - The 29-result fixture is count-based rather than an exact stable contract, and the earlier 24 count is not reconciled

Paths:

- `src/Paradigm.Enterprise.Cli.Tests/CSharpCheckServiceTests.cs:51-93,97-111`
- `examples/beacon-ar/docs/beacon-ar-cli-repository-sql-policy/change-summary.md:13-20`

The test asserts a total of 29, broad message prefixes, API substring presence, and only two per-file counts. It does not assert the complete expected `(code, severity, fully-qualified member, construct, file, line, column)` set. One missing sink and one extra/mislocated declaration can therefore preserve the count and pass. The summary records 29 but does not explain why the earlier review evidence reported 24 or identify the five added tuples.

Smallest safe correction: replace the broad assertions with an explicit ordered expectation for every diagnostic tuple, retain repeated-run equality and duplicate checks, and add a short evidence reconciliation naming which five cases changed 24 to 29 and why. Update the expected total after the P1 fixtures are added.

### P2 - PE7004 continuation lacks an unrelated-configuration regression assertion

Paths:

- `src/Paradigm.Enterprise.Cli/Commands/Checks/ChecksRunCommandHandler.cs:19-24`
- `src/Paradigm.Enterprise.Cli.Tests/CSharpCheckServiceTests.cs:131-166`

The handler change correctly permits semantic execution in the presence of `PE7004`, and the PE3107 expiry fixture passes. However, the new test does not assert `checks.executed`, does not cover an expired suppression for another diagnostic, and does not prove that an unrelated `PE5001` configuration error still prevents execution. The code is narrow, but its intended behavior is not fully locked down.

Smallest safe correction: add CLI-level assertions that expired suppressions retain `PE7004`, execute checks, and expose the underlying diagnostic regardless of code, while malformed configuration retains `PE5001`, leaves built-in checks unexecuted, and preserves the existing error exit.

## Verified clean Task 6 areas

- Repository identity is semantic `IRepository` assignability, including indirect interfaces and partial declarations; it does not use repository names, folders, or Beacon allowlists.
- Evaluated `IsTestProject=true` is the explicit project-level exception. Checked-in generated production source is not blanket-excluded.
- EF raw/interpolated families, direct ADO construction/text/execution, current Dapper/equivalent fixtures, typed SQL Server/PostgreSQL procedure wrappers, EF CRUD/LINQ, ordinary business executors, and non-repository/test controls behave as documented in the current fixture.
- Diagnostics are errors with fully qualified members, stable source coordinates, deterministic code/location/message ordering, and exact duplicate elimination. No fixer or new package was added.
- Expired suppression currently emits `PE7004` and restores the suppressed PE3107; invalid non-PE7004 configuration errors remain blocking by inspection.
- CLI documentation, maintainer documentation, changelog, coding/database references, and repository/database/review/evolution skills state the general policy without application-specific exceptions.

## Final cross-task Beacon audit

- The canonical `BeaconAr.slnx` has the six responsibility folders and unique responsibility placement; the database remains a SQL Server module and the finite generators/bootstrap projects are tools.
- Every database view source/object found ends in `View`; Operations audit/idempotency views are present. The Data/Domain/Providers capabilities align to Access, MasterData, Operations, and Sales, with exactly those four DbContexts and no Receivables context.
- Beacon production repositories use typed routines/EF and the repository source scan found no raw SQL APIs or SQL statement text. The official-derived T4 boundary and provenance record remain intact.
- Behavior partials are co-located, broad static validators are removed, system enums are in Interfaces with recorded seed parity, canonical generated views own search/read shapes, and the retained nested Sales DTO exception has a documented external-contract reason and removal trigger.
- Task 5's final review is approved. Six search actions bind one `[FromQuery]` request; hosted tests cover binding/defaults/errors/authorization; Provider contracts are honest command/workflow surfaces; entity/request/Provider/controller validation ownership is documented. Its fresh SQL Server evidence remains 24/24 database and 60/60 API tests with task-owned Docker cleanup and pre-existing Aspire resources untouched.
- Tasks 1-5 each contain plan, decisions, change summary, and final approved review feedback. Task 6 has plan, decisions, change summary, and this nonapproval review. The Task 6 branch is intentionally uncommitted/unmerged pending correction.

## Independent validation

- Focused `CSharpCheckServiceTests`: 9/9 passed.
- Framework Release build: passed with 0 warnings and 0 errors.
- Framework non-integration suites: 126/126 framework tests and 86/86 CLI tests passed.
- At this historical pre-correction review point, the direct source-CLI run over `SemanticViolations` produced exactly 29 PE3107 diagnostics, plus the fixture's established unrelated PE3103-PE3106 diagnostics.
- `git diff --check`: passed with line-ending conversion warnings only.
- No Docker resource was created or removed by this review; Task 5 live evidence was reused as planned.

Approval requires correction of the three P1 coverage holes, exact fixture assertions/count reconciliation, and the bounded PE7004 regression coverage, followed by the same focused/full/packed/baseline gates already listed in the implementation plan.

## Cycle 2 re-review

### Status

**Not approved.** All five cycle-1 findings were materially addressed, but one equivalent-extension false negative remains and the source-generator fixture introduced an unapproved direct package dependency.

### Resolved - post-generator compilation coverage

`CSharpChecks` now passes `compilation.SyntaxTrees` after `RunGeneratorsAndUpdateCompilation`, while the established layout checks continue to inspect only evaluated source trees. The real `RepositorySqlGenerator` incremental generator emits `SourceGeneratedRawSqlRepository`; its exact `SourceGeneratedRawSqlRepository.g.cs(8,25)` diagnostic is in the ordered manifest. The final count contains one copy of every original diagnostic, so post-generator analysis did not duplicate evaluated trees.

### Resolved - conditional receiver recovery and misleading parameter names

`ReceiverType` now uses `IInvocationOperation`, including the operation instance and ordinal-zero extension receiver. Exact fixtures cover conditional `DbConnection.CreateCommand`, `DbCommand.ExecuteReader`, Dapper `QueryAsync`, and the equivalent `ExecuteStatementAsync`. A connection extension whose non-string parameter is misleadingly named `sql` stays clean, as does the same SQL-looking string passed through a non-database receiver.

### Resolved - repository-owned post-declaration assignments

Assignments to fields/properties whose containing type is the repository now inspect the right-hand expression. The manifest contains four constructor assignments covering direct, compound, coalescing, and helper-returned SQL. The prose-only conditional assignment remains clean. Constructor diagnostics use the stable repository constructor name.

### Resolved - exact diagnostic contract and count reconciliation

`Repository_SQL_policy_reports_semantic_sinks_and_declarations_without_false_positives` now asserts an explicit ordered set of all 39 `(code, severity, member, construct, file, line, column)` tuples. Repeated-run equality, deterministic ordering, and duplicate elimination remain separate assertions. `change-summary.md` precisely reconciles 24 to 29 by naming the five added pre-correction cases, then 29 to 39 as four conditional sinks, four assignments, one helper declaration, and one emitted source-generator declaration.

### Resolved - PE7004 execution and unrelated invalid configuration

Active and expired PE3107 suppressions assert 38/39 findings and `checks.executed=["csharp"]`. An expired unrelated PE3103 suppression emits PE7004, restores PE3103, and executes checks. A malformed suppression emits PE5001, preserves exit code 1, emits no PE31 result, and leaves the executed-check list empty. The handler change remains limited to allowing PE7004 through the existing blocking-error gate.

### P1 - Equivalent connection extensions still depend on statically recognizable argument contents

Path: `src/Paradigm.Enterprise.Checks.CSharp/Analysis/RepositorySqlPolicy.cs:176-184,202-215,282-328`

The resolved extension is reported only when a string/FormattableString argument also satisfies `IsSqlExpression`. Consequently, a repository can accept or obtain runtime command text and call `connection.ExecuteStatementAsync(runtimeStatement)` without PE3107: the parameter/property has no foldable SQL declaration, so `hasSqlArgument` is false. A resolved `Query*`/`Execute*` extension over `DbConnection`/`IDbConnection` with a string/FormattableString command parameter is itself the prohibited raw-command sink; its safety cannot depend on whether the current argument happens to be a compile-time SQL literal. The new fixture that treats `connection.ExecuteStatementAsync("Select the reviewed option.")` as allowed encodes this under-enforcement.

Smallest safe correction: after the stored-procedure and Dapper handling, classify an equivalent `Query*`/`Execute*` connection extension from its resolved connection receiver and string/FormattableString command parameter, independent of argument foldability. Retain the negative non-string-`sql` and non-database-receiver fixtures. Add a repository method whose command string is a parameter and require its exact PE3107 tuple; remove the prose-on-database-command allowance.

### P2 - The real-generator fixture adds a direct NuGet dependency without approval or an accurate dependency record

Paths:

- `src/Paradigm.Enterprise.Cli.Tests/Fixtures/RepositorySqlSourceGenerator/RepositorySqlSourceGenerator.csproj:12`
- `examples/beacon-ar/docs/beacon-ar-cli-repository-sql-policy/implementation-plan.md:12`
- `examples/beacon-ar/docs/beacon-ar-cli-repository-sql-policy/change-summary.md:20`

The new fixture project adds a direct `Microsoft.CodeAnalysis.CSharp` `PackageReference`. The version is already centrally governed and used by the production checks project, but repository guidance explicitly says an existing dependency elsewhere does not approve a new project coupling, and this task's plan said not to add a package. The summary's statement that no package was added is therefore inaccurate at the project-dependency level.

Smallest safe correction: either reuse an already approved repository-owned generator test boundary without adding a new direct dependency, or obtain/document explicit approval with package purpose, MIT license/source, alternatives, and transitive/security posture. In either case, make the change summary distinguish a new package identity/version from a new direct project dependency.

### Cycle 2 validation and cross-task acceptance

- Independent focused `CSharpCheckServiceTests`: 11/11 passed.
- Independent framework Release build: passed with 0 warnings/errors.
- Independent non-integration suites: 126/126 framework and 86/86 CLI tests passed.
- The implementation record reports 20/20 packed integration tests, installed package policy/audit, skill/plugin validation, and Docfx. This bounded re-review did not repack because the remaining defect is visible in the semantic predicate and fixture contract.
- The recorded Beacon project-scoped checks and RDX baseline remain at zero PE3107. The policy contains no Beacon/RDX path or name exception; the remaining gap is generic runtime command text.
- The final cross-task Beacon audit remains clean and unchanged from cycle 1. Task 5's 24/24 database and 60/60 API evidence is still the applicable fresh live gate. No Beacon application/database/generated artifact changed and no Docker resource was created or removed in this cycle.
- `git diff --check` remains clean apart from line-ending conversion warnings. The Task 6 branch remains intentionally uncommitted/unmerged pending approval.

Approval now requires closing the runtime-string equivalent-extension gap and resolving/documenting the new direct package dependency, then updating the exact tuple count and rerunning the already established focused/packed/baseline gates.

## Cycle 3 final re-review

### Status

**Approved.** Both remaining cycle-2 findings are resolved. No open Task 6 or cross-task Beacon finding remains.

### Equivalent runtime command extensions - resolved

`RepositorySqlPolicy.RawInvocation` now classifies a resolved `Query*`/`Execute*` extension over `DbConnection`/`IDbConnection` when its signature contains a `string` or `FormattableString` parameter, independent of whether the current argument is constant, interpolated, or available only at runtime. Stored-procedure receivers remain excluded first and Dapper-family calls retain their explicit resolved-symbol path.

The exact manifest now includes both previously missing runtime-value forms:

- direct `DbConnection.ExecuteStatementAsync(string runtimeStatement)`;
- conditional `IDbConnection.QueryContractInterpolatedAsync(FormattableString runtimeQuery)`.

The conditional literal extension remains covered. The non-string parameter misleadingly named `sql` and the SQL-looking string on a non-connection business receiver remain clean. The former prose-on-connection allowance was correctly removed because a resolved raw-command sink is prohibited regardless of its current argument contents.

### Generator dependency boundary - resolved

`RepositorySqlSourceGenerator.csproj` contains zero `PackageReference` items. It compiles against the already restored, centrally governed Microsoft.CodeAnalysis Common 5.0.0 asset and uses a non-output project edge to the existing approved checks project to establish restore/build ordering. No package identity or version was added, and no package lockfile changed. `src/Directory.Packages.props` only names the already-existing Roslyn version once through `MicrosoftCodeAnalysisVersion`; the production `Microsoft.CodeAnalysis.CSharp` package remains the sole direct Roslyn package reported by package policy.

The focused Release run built and loaded the real netstandard2.0 incremental generator, and the manifest still contains exactly one emitted `SourceGeneratedRawSqlRepository.g.cs(8,25)` result. The change summary's no-new-package claim is now accurate and explicitly distinguishes package identity/version/lock state from reuse of an approved restored compiler asset.

### Exact contract and validation

- The ordered manifest contains exactly 41 unique tuples. The documented reconciliation is complete: 24 to 29 names five cases, 29 to 39 names ten review corrections, and 39 to 41 names the two runtime-value extension sinks. No earlier tuple was removed.
- Independent focused `CSharpCheckServiceTests`: 11/11 passed.
- Independent framework Release build: passed with 0 warnings/errors.
- Independent source-CLI `packages check` for `src/Paradigm.Enterprise.slnx`: success with zero diagnostics; the generator fixture introduces no direct package row.
- `git diff --check`: passed with line-ending conversion warnings only. No package lockfile is present in the Task 6 diff.
- Cycle 2 independently passed 126/126 framework and 86/86 CLI non-integration tests. The implementation record's 20/20 packed-tool run remains applicable because the final correction is fully exercised through the same compiled checks assembly and exact source-service fixture; no packaging surface changed.
- Recorded Beacon project-scoped and RDX baselines remain at zero PE3107. No path/name allowlist, suppression, or generated-code exemption was added.

### Final Beacon acceptance

The cross-task audit remains accepted: canonical solution folders, universal View suffix and Operations views, four context-aligned capability boundaries, typed routine/EF repository ownership, official-derived T4 provenance, co-located entity behavior, Interfaces-owned system enums and seed parity, canonical view read shapes with the documented Sales detail compatibility exception, honest Provider/API contracts, complex query-object binding, authorization/security, deterministic OpenAPI/client/generation artifacts, and complete task plan/decision/change/review chains are all intact.

Task 5's fresh 24/24 database and 60/60 API evidence remains the live SQL Server gate. Task 6 changed no Beacon application, database, generated persistence, mapper, OpenAPI, or client artifact. No Docker resource was created or removed during any Task 6 review cycle, and the documented pre-existing Aspire resources remain untouched.

Task 6 is approved for commit and merge into `new-example-from-skills`.
