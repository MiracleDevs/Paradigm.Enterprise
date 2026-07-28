# Documentation site refresh handoff

## Task

The task replaces the user-facing documentation with a curated Docfx site, corrects the repository entry points, and adds validation and GitHub Pages publishing. Runtime library code, public package contracts, and project target frameworks are outside the scope.

The task branch is `docs/docfx-site-refresh`. Both it and the local integration branch `agent-skills` were created from `main` at `60a7306156193998a06bdcaeb083e2881b2db200`.

## Plan review

An independent reviewer inspected the plan before tracked files were edited. The reviewer approved the branch workflow, privacy controls, Docfx 2.78.5 choice, source-verification strategy, and uncommitted-diff review gate.

The reviewer required the security documentation to state two details explicitly. The generic controller bases carry `AllowAnonymous`, and endpoint exposure control only restricts routes after it is enabled. Endpoint exposure is not authentication or authorization. These requirements are part of the implementation scope.

## Implementation review

The implementation reviewer was independent from the implementer and the plan reviewer. The reviewer inspected the full uncommitted diff, the current library source, the generated site, and the publishing workflow.

The first material finding concerned controller security. The initial draft incorrectly suggested that `Authorize` or a fallback policy could secure a controller derived from the library bases. The corrected documentation now explains that inherited `AllowAnonymous` metadata bypasses ordinary authorization. The protected vertical-slice example derives directly from ASP.NET Core's `ControllerBase`, injects its provider, and applies authorization there. Architecture, Web API, security, testing, conventions, and troubleshooting pages received the same correction.

The source audit found inaccurate descriptions of the standalone generator, audit contracts, table-file configuration, stream requirements, provider hooks, and dependency discovery. The generator guide now records the current dual-use path defect, coupled execution, swallowed failures, OpenAPI prerequisites, and SQL Server-only mapper output. PostgreSQL guidance uses manual `INpgsqlParameterMapper` registration. The audit contract text now distinguishes user identifiers from timestamps. Table-file guidance records the writer-only XML configuration, unused `IndentResults`, and the seekable output requirement. Provider guidance now distinguishes view and entity hooks, bulk save behavior, and the pre-commit delete after-hook. Discovery guidance now separates concrete marker filtering from exact-name interface registration.

The site and workflow audit found a case-sensitive PostgreSQL solution path, insufficient Pages permissions, a hosted API link without an index page, incomplete dark-theme styling, unstyled sequence diagrams, and narrow-screen table overflow. The solution path and workflow were corrected, the README now links to the generated API landing page, the CSS follows Docfx's theme attribute and supports horizontal table scrolling, and both sequence diagrams have restrained Mermaid theme variables. The workflow action majors were also updated after checking their current official releases. A PowerShell verification exposed an invalid semicolon-delimited MSBuild argument; the README, contributor guide, and workflow now use MSBuild percent escaping.

The reviewer verified the corrected material findings and reported no remaining content, code, or workflow findings.

The independent implementation reviewer rechecked the full corrected uncommitted diff and this handoff after all material findings were applied. The reviewer found no remaining material or minor findings and approved the implementation for commit and merge, subject only to a final verification that this approval record is the sole subsequent change.

## Verification

The final sequential verification used .NET 10 and the repository-local Docfx 2.78.5 manifest.

| Gate | Result |
| --- | --- |
| Branch base | `main`, `agent-skills`, and `docs/docfx-site-refresh` originated at `60a7306156193998a06bdcaeb083e2881b2db200`; implementation remained uncommitted during review |
| Library restore | Succeeded |
| Release library build | Succeeded with 31 existing warnings and no errors; 30 are stored-procedure `CS8619` warnings and one is `MSTEST0032` |
| Library tests | 126 passed, 0 failed, 0 skipped |
| Compatibility example | Restore and Release build succeeded with no warnings or errors |
| Docfx tool restore | Restored pinned version 2.78.5 |
| Docfx build | Succeeded with warnings treated as errors; 0 warnings and 0 errors across all 13 non-test assemblies |
| Generated output | 244 HTML files, including 215 API files; navigation, search index, sitemap, custom CSS, icon, and API landing page present |
| Mermaid | Exactly four curated diagrams; bundled runtime present and generated pages contain Mermaid blocks |
| API source links | Generated API pages link to repository source; 158 API pages contain source links |
| Site exclusions | RFCs, rules, migration records, handoffs, generated API metadata, and `_site` sources are excluded or ignored as intended |
| Markdown links | All curated local links and README links resolve |
| Content scans | No external example identifiers, proprietary namespaces, em dashes, emojis, mojibake, or separator-only lines found |
| Formatting | `git diff --check` passed |
| Scope | Working tree contains only intended documentation, workflow, solution-path, ignore, and XML-comment corrections |

One Docfx invocation encountered a transient output-file lock while reviewer inspection was running concurrently. The final sequential Docfx invocation completed successfully with no warnings or errors.
