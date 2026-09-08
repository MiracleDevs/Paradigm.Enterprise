# Beacon AR CLI Repository SQL Policy — Decisions

## Settled decisions

1. **Enforcement surface:** `PE3107` is a compiled Roslyn diagnostic under `paradigm checks run`, not a metadata `validate` rule. Raw-SQL ownership requires source expressions, resolved overloads, containing symbols, and constant values that restored assembly metadata cannot supply.

2. **Severity and failure mode:** `PE3107` is an error. Semantic compilation must be complete before the rule runs; unresolved symbols cause the existing fail-closed analysis error rather than a syntax/name heuristic that could silently miss or misclassify database calls.

3. **Repository identity:** scope is exact assignability to `Paradigm.Enterprise.Domain.Repositories.IRepository`, including indirect bases/interfaces and all partial declarations. Names and folders are not evidence. An evaluated `IsTestProject=true` is the explicit test exception.

4. **Boundary:** production repositories may use EF/LINQ for simple bounded work and may call typed SQL Server/PostgreSQL stored-procedure wrappers. They may not contain EF raw SQL, ADO command construction/text/execution, Dapper-like SQL execution, or repository-owned SQL strings/helpers. SQL remains in database projects; typed procedure plumbing remains in Data outside repository implementations.

5. **False-positive policy:** API decisions use resolved symbols and receiver/parameter types. SQL-string detection is conservative and requires statement structure; names such as `ExecuteAsync`, procedure object names, ordinary prose, and query values are insufficient. No application-name/path allowlist is permitted.

6. **Suppression governance:** reuse `.paradigm/config.json` suppressions. The stable fully qualified member in the message supports symbol matching; the source coordinate supports location matching. Every suppression still requires a reason and expiry, and expiry emits `PE7004` while restoring the underlying `PE3107` error.

7. **No fixer:** there is no automatic rewrite. Choosing EF versus a database routine, designing parameters/results, transaction behavior, locking, and generation cannot be safely inferred from a raw string.

8. **Generated source:** production generated repositories are not automatically excluded. `PE3107` runs on the post-generator compilation tree set; a checked-in `.g.cs` file is retained as one case, while a real incremental-generator fixture proves emitted repositories are also analyzed. Generation is not proof that embedded SQL is architecturally safe; use the safe boundary or an explicit expiring suppression with evidence.

9. **Baseline evidence:** current Beacon AR, Paradigm Web API template, and RDX DMS repository implementations use typed stored-procedure wrappers and contain no raw SQL. They are regression baselines, not special cases in the implementation.

## Ambiguities resolved for implementation

- **“Validate SQL queries”** could have meant the metadata `validate` command or SQL syntax validation. It is interpreted as an architectural source policy because the requested prohibition is about where SQL is authored. `PE3107` does not parse or execute SQL and does not replace database-project validation.
- **“Dapper-like”** has no single base contract. Detection is limited to resolved Dapper symbols or resolved `Query*`/`Execute*` extension methods with a database-connection receiver and a string/`FormattableString` command parameter. Roslyn operations recover conditional-access receivers; parameter names and argument foldability are not policy evidence. The resolved raw-command API is prohibited even when its argument is a runtime value. This avoids flagging unrelated business APIs and a connection extension whose only command-like parameter is non-string while covering equivalent micro-ORM calls.
- **SQL helper strings** can resemble prose. The implementation should flag strong SQL statement structure and all values reaching known raw sinks, but should not attempt general interprocedural taint analysis in this task. A future missed pattern with a reproducible fixture is the signal to extend the semantic rule.
- **Assignments after declaration** are covered only when the resolved target is a field/property owned by the repository and the right-hand expression is constant/foldable SQL or a directly resolved repository helper returning it. This closes ordinary initialization/assignment bypasses without introducing general data-flow taint analysis.
- **Tests and infrastructure** need SQL legitimately. Test-project exclusion is based on evaluated project metadata, while database scripts, bootstrap, and generator code remain allowed by not being production `IRepository` types. A production repository placed in a tool project is still covered if it implements `IRepository`.
- **External baseline execution** may be blocked by unavailable restore feeds, credentials, Docker, or local reference state. Such gaps must be recorded with the exact failed command; they are not grounds for a path exception or a weakened diagnostic.
