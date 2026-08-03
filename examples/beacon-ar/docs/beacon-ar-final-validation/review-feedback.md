# Beacon AR final validation findings

> Historical record: commands and solution references below describe validation before the migration to root `BeaconAr.slnx`. Use `BeaconAr.slnx` for current work.

Date: 2026-08-02  
Audit range: user-request baseline `8a018ca` through merged Web API head `aaa5081`

## Findings before remediation

### P2 — duplicate master-data read contracts remain after generated views became canonical

- Evidence: `ProductDto`, `CustomerDto`, `AddressDto`, and `CarrierDto` remain in Domain; each master-data Provider exposes a legacy DTO method set that delegates to a parallel `*ForApiAsync` generated-view method set and maps the view back to the DTO. Only repository tests, provider tests, and live/HTTP acceptance fixtures call the DTO methods. The checked OpenAPI artifact and generated TypeScript client already expose `ProductView`, `CustomerView`, `CustomerAddressView`, and `CarrierView`.
- Consequence: application callers see two names and two shapes for the same read operation, Provider mapping code duplicates the generated view contract, and compile-time entity/view interface alignment no longer guarantees the compatibility DTO surface.
- Smallest safe correction: delete the four DTO records and their `ToDto` mappers, make the generated-view methods the single custom Provider contract, rename the `*ForApiAsync` methods to the ordinary operation names, and update controllers/tests to consume views. Convert `RowVersion` to the canonical base64 token only where tests or `If-Match` transport construction require a string.
- Compatibility: this removes an internal example-only Provider surface. It does not change the published HTTP/OpenAPI/TypeScript contract because that contract already uses generated views.

### Corrected audit inputs — verified

- The implementation plan incorrectly labeled `f8ad539` as the audit baseline. The current user-request baseline is `8a018ca`; older history remains useful provenance only.
- Secret-safe `.env` inspection found the ignored file present with nonblank explicit connection, managed password, mode, and publication keys. Values were not printed. The existing explicit target is not trusted or authorized for publication; live validation will use only uniquely named resources created and owned by this task.

## Confirmed intentional divergence from the reference repository

The RDX DMS reference demonstrates an explicit composition root, dependency registration before convention discovery, Swashbuckle document/UI setup, a root route, and authentication before authorization. Beacon AR retains those precedents. It intentionally does not copy the reference's deprecated AzureAD UI dependency, broad allow-any CORS, custom API-key/token middleware, or legacy anonymous generic-controller assumptions; Beacon AR uses Microsoft Identity Web bearer validation, constrained CORS, direct protected `ControllerBase` controllers, delegated-user actor classification, safe Problem Details, and generated database views.

Further findings and final dispositions are recorded below as the audit proceeds.

### P2 — idempotency orchestration classifies SQL Server exceptions in Providers

- Evidence: `CreationIdempotencyProvider` imports `Microsoft.Data.SqlClient` and inspects error numbers `2601`/`2627` directly. Other persistence conflict classifiers correctly keep EF/SQL knowledge in Data behind Domain-owned contracts.
- Consequence: business orchestration depends on a concrete database driver and a broad exception-name fallback can misclassify unrelated exceptions as an idempotency-key race.
- Smallest safe correction: introduce a narrow Domain contract for identifying the reviewed idempotency unique constraint, implement it in Data with SQL Server-specific evidence, inject it into the Provider, and register it at the composition root.
- Compatibility: internal dependency correction only; HTTP and schema contracts do not change.

### P1 — `start.sh doctor` fails after a normal Windows checkout

- Evidence: `bash ./start.sh doctor` fails at line 2 with `pipefail\r: invalid option name`; `git ls-files --eol start.sh` reports an LF index with a mixed-ending worktree and no applicable EOL attribute.
- Consequence: the documented starter workflow is unusable from Git Bash on the supported Windows development path, before prerequisite checks can run.
- Smallest safe correction: add an example-scoped `.gitattributes` rule enforcing LF for `*.sh`, normalize `start.sh`, and rerun doctor plus Aspire restore.
- Compatibility: source-control normalization only; command behavior is unchanged.

### P1 — the live integration fixtures omit production coordinators

- Evidence: the first task-owned SQL run executes all 22 database tests and reports 4 passed/18 failed because `SalesWorkflowCoordinator` is absent from `SalesWorkflowLiveTests.CreateServices`. After that correction, a fresh run reports 12 passed/10 failed because `MasterDataMutationCoordinator` is likewise absent from `MasterDataLiveTests.CreateServices`. Offline runs skipped these tests and could not expose either stale composition.
- Consequence: the full live transaction, concurrency, snapshot, status-history, conversion, and dashboard matrix cannot run, so prior offline green results did not validate those release claims.
- Smallest safe correction: register the same scoped coordinators used by production in their respective live fixtures and rerun the complete database executable against a fresh task-owned SQL Server.
- Compatibility: test composition only; production composition already registers the coordinator.

### P1 - acceptance serializer replaced its fallback resolver

- Evidence: after fixture repair, the database suite passed 22/22 while all 16 live API scenarios that serialize anonymous request objects failed before dispatch with `NotSupportedException`. `BeaconArApiAcceptanceTests.CreateJsonOptions` assigned the source-generated app resolver as the only resolver.
- Consequence: the test client could not send its test-only anonymous payloads, masking actual live HTTP behavior.
- Smallest safe correction: insert the app resolver first and add `DefaultJsonTypeInfoResolver` as the test-client fallback, matching production's resolver-chain model while exercising app-owned view property renames.
- Regression: a fresh disposable run passed WebApi 56/56 and Database Integration 22/22.

### P2 - generated TypeScript contained trailing whitespace

- Evidence: deterministic client regeneration was byte-stable, but `git diff --check` reported trailing spaces in NSwag optional-parameter comments.
- Consequence: the authoritative regeneration path could not satisfy repository hygiene without hand-editing generated output.
- Smallest safe correction: normalize line endings and trim line ends in `OpenApiTypeScriptGenerator` before writing, then regenerate twice.
- Regression: both normalized runs hash to `0d77424cd59efd63cbb8ea0ae05a20684a79a1da59c110c8cfdfe4293a468e90`; strict TypeScript and `git diff --check` pass.

### P1 - Visual Studio restored a SQL project identity that disagreed with the solution

- Evidence: active Visual Studio 18 restored `TargetDatabaseSet` and project GUID `{A09E027B-4428-49A6-9A92-C43CF35D9206}` within 500 ms after two safe removal attempts, while `BeaconAr.sln` still used `{36012A89-DEB7-49AF-AA8F-2C0B1C3289BF}`.
- Consequence: project and solution identities could diverge even though command-line compilation succeeds, undermining the required Visual Studio experience.
- Smallest safe correction: retain the empirically IDE-owned metadata and replace the solution project entry, all 12 build mappings, and the solution-folder mapping with the same GUID; restore the final newline.
- Regression: zero old-GUID hits, 14 expected aligned solution hits, one SQL project identity, no duplicate solution GUID, stable newline after Visual Studio/build/validator activity, SQL build 0 warnings/errors, strict validation zero diagnostics.

## Final dispositions

1. Duplicate master-data DTOs: resolved. Four DTO records, compatibility methods, and `ToDto` helpers are removed; generated views are canonical through HTTP/client generation.
2. SQL classification in Providers: resolved. Domain owns `IIdempotencyPersistenceErrorClassifier`; Data recognizes only SQL 2601/2627 for `UQ_IdempotencyRequest_UserId_Operation_KeyHash`; Providers remain driver-free.
3. Windows starter line endings: resolved. Example `.gitattributes` enforces LF, the worktree is normalized, Git Bash doctor and Aspire restore pass.
4. Live fixture composition: resolved. Both production coordinators are registered; the full disposable live matrix passes.
5. Acceptance resolver chain: resolved with explicit reflection fallback only in the test client.
6. Generated client hygiene: resolved in the owning generator and verified deterministic.
7. Visual Studio SQL identity: resolved by aligning every solution reference to the IDE-restored project GUID and documenting the superseding decision.

## Final reviewer evidence

- Locked restore, SQL project Release build, and full solution Release build pass with 0 warnings and 0 errors.
- Offline tests: 164 total, 123 succeeded, 0 failed, 41 expected live skips.
- Disposable SQL: Database Integration 22/22 and WebApi 56/56; all task-owned resources removed (`0/0/0`).
- EFPT: first/restart bootstrap pass; normal generations exit 0 and match; recovery/redaction fixtures pass. The retained single generated context diff is reviewed schema collation metadata, not manual output.
- OpenAPI: two identical canonical hashes `dd12b0365570e8bd1662ae0040a6384f969792717dc7fb95a7456ea2a1650150`, zero CR bytes, exactly one final LF, and valid JSON. TypeScript: two identical normalized hashes `0d77424cd59efd63cbb8ea0ae05a20684a79a1da59c110c8cfdfe4293a468e90`; strict compilation passes.
- Paradigm doctor/package check/audit/validate/strict database validation and focused semantic checks pass. Audit has no vulnerability/deprecation result and 14 update-only warnings. The known Aspire `Projects` solution-check limitation is documented separately.
- Skill/plugin validators pass. Stale abstraction and tracked secret/machine-artifact scans are clean. `git diff --check` passes.

No unresolved product-code release finding remains. Deployment-owner limitations are explicit in `decisions.md` and do not justify targeting the untrusted external `.env` database.

## Independent final release review

Reviewer date: 2026-08-02  
Reviewed ranges: complete request `8a018ca` through the current worktree; Task 5 `aaa5081` through the current worktree

### P2 - OpenAPI generation is platform-dependent at the owning script boundary

- Evidence: `scripts/generate-openapi.ps1:68` writes `$content.TrimEnd() + [Environment]::NewLine`. The current generated artifact ends in bytes `0A 7D 0D 0A`; its Windows hash is `3c828b18a0ca495ac85a1dfe8126a18ed5c212428855d385741bcd83fa423f0c`. Replacing only the final CRLF with the LF that the same PowerShell script emits on Linux produces `dd12b0365570e8bd1662ae0040a6384f969792717dc7fb95a7456ea2a1650150`. Two local generations are identical, but they do not prove cross-platform determinism.
- Consequence: Windows and Linux contributors or CI can regenerate different authoritative OpenAPI bytes without any schema change. The recorded hash becomes host-specific and can cause false contract drift; downstream client generation happens to parse either newline but cannot repair the source artifact.
- Smallest safe correction: make `generate-openapi.ps1` normalize response line endings and append a literal LF, regenerate OpenAPI twice, regenerate the TypeScript client twice, run strict client compilation and `git diff --check`, and update the recorded OpenAPI hash. Add a deterministic assertion that the artifact contains no CR bytes so the defect cannot recur.
- Compatibility: artifact bytes and the recorded hash change by line-ending normalization only; routes and schemas should remain unchanged.

### P3 - live SQL counts are credible primary-executor evidence, not independently reproducible release evidence

- Evidence: the task documents unique disposable resources, Database Integration 22/22, WebApi 56/56, two EFPT passes, and cleanup. The ignored Visual Studio test store contains the relevant live test identities, and this independent review confirmed zero matching containers, networks, or images. However, no portable secret-safe TRX or sanitized command transcript containing the final pass counts remains in the task folder, so this reviewer did not independently re-derive those counts and did not recreate external resources.
- Consequence: the cleanup claim is independently verified and the live result is credible, but future reviewers must trust the primary executor's summary for the exact counts. This is an evidence-retention gap, not evidence of a product-code failure.
- Smallest safe correction: either attach a sanitized TRX/CI artifact for the already recorded run, or qualify the documents consistently as primary-executor evidence. For future disposable runs, retain a secret-reviewed summary/TRX outside source control when raw output could contain credentials; never weaken redaction or commit connection strings merely to satisfy traceability.

### Independently reproduced evidence

- Locked restore passed; Release solution build passed with 0 warnings/errors; offline tests passed exactly 164 total, 123 succeeded, 41 expected live skips, 0 failed.
- SQL project Release build and strict database validation passed. Evaluated items are 37 `Build`, 11 `None`, one `PreDeploy`, and one `PostDeploy`, with no duplicate or cross-item identities. The solution contains the exact 14 reviewed SQL project GUID references, no old GUID, and no duplicate project GUID.
- Legacy master-data DTO/mapper scans are clean outside tests that prohibit them. Providers contain no source-level SQL/EF driver dependency, public `IQueryable` boundaries are absent, and the idempotency classifier recognizes only SQL 2601/2627 with the exact reviewed constraint name.
- Paradigm doctor, package check, validate, strict database validation, and focused Domain/Data/Providers/WebApi semantic checks passed. Package audit reproduced only the documented 14 update-only warnings. Solution-wide semantic checking reproduced only the documented Aspire generated `Projects` limitation with exit 3.
- EFPT failure-recovery/redaction fixtures, Git Bash doctor (5 pass, one known certificate warning, 0 fail), Aspire restore, skill validation (11), plugin validation, and both Task 5 and complete-baseline `git diff --check` passed.
- TypeScript generation is byte-stable at `0d77424cd59efd63cbb8ea0ae05a20684a79a1da59c110c8cfdfe4293a468e90`; `npm ci` found zero vulnerabilities and strict `tsc` passed with only the documented local Node patch-floor warning. `start.sh` is LF in index/worktree with the expected `text eol=lf` attribute.
- Secret and tracked machine-artifact scans found no release leak. The two secret-pattern source matches are the AppHost secret-parameter flow and a deliberate password-handling test sentinel.

Independent disposition: changes requested for the P2 OpenAPI generator defect. No additional production-code defect was found; the P3 live-evidence limitation should be resolved or explicitly qualified before final release approval.

## Final implementer response to independent review

### P2 OpenAPI platform-dependent bytes - resolved

- `scripts/generate-openapi.ps1` now normalizes response CRLF and CR to LF and appends a literal PowerShell LF. It no longer uses `[Environment]::NewLine`; the example `.gitattributes` also enforces LF for the authoritative OpenAPI JSON after checkout.
- `OpenApiArtifactTests.GeneratedDocumentIsACompleteClientContract` now rejects every CR byte, requires exactly one final LF, parses the authoritative bytes as JSON, and pins the reviewed SHA-256.
- Two OpenAPI regenerations are identical at `dd12b0365570e8bd1662ae0040a6384f969792717dc7fb95a7456ea2a1650150`. Two downstream TypeScript regenerations remain identical at `0d77424cd59efd63cbb8ea0ae05a20684a79a1da59c110c8cfdfe4293a468e90`; strict TypeScript and diff checks pass.

### P3 independently reproducible live evidence - resolved

- A fresh task-owned target `beacon-ar-task5-evidence-de0118754268` passed first bootstrap, restart bootstrap, Database Integration 22/22, and WebApi 56/56.
- [Sanitized live evidence](evidence/live-sql-2026-08-02.md) contains exactly 22 database and 56 API fully qualified Passed entries. It contains no connection/environment values, raw output, error streams, stack traces, or credentials.
- Raw TRX and the temporary extraction runner were deleted. Exact owned cleanup is containers/networks/images `0/0/0`.

Independent-review P2 and P3 now have complete final dispositions; no requested review change remains open.

## Independent final approval after remediation

Reviewer date: 2026-08-02

The P2 and P3 findings are resolved and the Task 5 change is approved.

- OpenAPI ownership is corrected at `scripts/generate-openapi.ps1`: response CRLF and lone CR are normalized to LF and a literal LF is appended. The example `.gitattributes` applies `text eol=lf` to the authoritative JSON.
- Independent byte inspection reports zero CR bytes, exactly one trailing LF, no UTF-8 BOM, valid JSON, and SHA-256 `dd12b0365570e8bd1662ae0040a6384f969792717dc7fb95a7456ea2a1650150`. The artifact test enforces those byte, parse, and hash invariants.
- Two independent OpenAPI generations reproduced the same `dd12b0365570e8bd1662ae0040a6384f969792717dc7fb95a7456ea2a1650150` hash. Two downstream TypeScript generations reproduced `0d77424cd59efd63cbb8ea0ae05a20684a79a1da59c110c8cfdfe4293a468e90`; strict `tsc` passed.
- The sanitized live record contains exactly 22 fully qualified Database Integration Passed rows and 56 fully qualified Web API Passed rows with complete 1-based sequences. First bootstrap, restart bootstrap, 22/22, 56/56, and cleanup `0/0/0` metadata each appear exactly once. No credential/connection material, raw output, error stream, stack trace, or TRX content is present.
- No raw TRX or temporary extraction runner remains in the workspace or matching temporary top-level paths. The task-owned Docker resource scan independently reports containers/networks/images `0/0/0` for all recorded Task 5 prefixes.
- Release solution build passed with 0 warnings/errors. Offline tests remain 164 total, 123 succeeded, 41 expected live skips, and 0 failed. Skill/plugin validation, documentation secret scan, and both Task 5 and complete-baseline `git diff --check` passed.

No unresolved product, test, documentation, generated-artifact, or release-evidence finding remains within the reviewed scope. The previously documented external Entra/Azure and Aspire semantic-check limitations remain explicit deployment/tooling limitations rather than Task 5 defects.
