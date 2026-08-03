# Beacon AR final validation change summary

> Historical record: commands and solution references below describe validation before the migration to root `BeaconAr.slnx`. Use `BeaconAr.slnx` for current work.

Date: 2026-08-02

## Outcome

The cross-cutting release audit is complete. It removed the remaining duplicate master-data DTO/provider surface, moved SQL Server idempotency-race classification out of Providers, repaired live fixture composition, made the Windows shell starter checkout-safe, reconciled stale generated persistence/client output, and added deterministic client normalization. No database or public route was redesigned.

The audit compared Beacon AR with `C:\Repositories\github\microsoft\rdx-dms-mvp\src\api`. It retained explicit composition, registration-before-discovery, Swashbuckle, an anonymous versioned root, and authentication before authorization. It intentionally rejected deprecated AzureAD UI, broad CORS, custom API-key/token middleware, and legacy anonymous controller assumptions.

## Requirement traceability

| Requirement | Owning implementation/evidence | Validation | Outcome / remaining risk |
| --- | --- | --- | --- |
| SQL Server project, solution visibility, scripts | `src/database/BeaconAr.Database.sqlproj`, `src/BeaconAr.sln`, architecture tests | locked restore; SQL project Release build; strict database validation; live Visual Studio rewrite observation | Pass, zero build warnings/errors and zero database diagnostics. Visual Studio-restored `TargetDatabaseSet`/`ProjectGuid` are retained; the solution entry, 12 build mappings, and folder mapping use the same non-duplicated GUID. No tracked `.sqlproj.user`, `.suo`, DACPAC/BACPAC, or publish profile. |
| Major/transactional views, naming, joins | `src/database/views`, generated `*View`, view repositories | schema/cardinality review; generated tests; 22/22 disposable SQL tests | Pass. Canonical names and common joined descriptions are exercised against SQL Server. |
| EFPT entities, interfaces, context, mapping | `efcpt-config.json`, T4 templates, generated Domain/Data, Interfaces | two normal disposable regenerations; recovery/redaction fixtures; architecture/domain tests | Pass. Both final runs hash to `1e2e044adf938857f790d9efb04800a250e307907d534d6c4bcecfdf453279a7`; one stale context was reconciled before stable run two. |
| Repository/provider ownership | master-data repository/provider contracts and implementations | Architecture 25/25; Providers 19/19; focused semantic checks | Pass. Repositories return entities/views; Providers own validation/mapping/orchestration. SQL-driver knowledge is in Data. |
| Official master-data CRUD bases | Product/Customer/Address/Carrier providers/repositories | framework CRUD tests plus 22/22 DB and 56/56 live API | Pass. Generated views are the sole read shapes; narrow request DTOs remain writes. |
| Quote/SalesOrder/conversion workflows | Sales providers, coordinators, repositories | live transitions, snapshots, rollback, races, idempotency, conversion cases | Pass. Custom provider/transaction boundaries are intentional because generic CRUD cannot express aggregate invariants. |
| API security, root, ETags, idempotency | WebApi Program/controllers/security/operations | 37/37 offline WebApi plus 56/56 live middleware/API; architecture tests | Pass. Root returns name/version; delegated users only; direct controllers prevent inherited anonymity; strong ETags and durable idempotency verified. Real tenant issuance remains deployment acceptance. |
| Swashbuckle/OpenAPI | filters, `.gitattributes`, and `artifacts/openapi/beacon-ar-v1.json` | two generations hash `dd12b0365570e8bd1662ae0040a6384f969792717dc7fb95a7456ea2a1650150`; byte regression checks zero CR and one final LF | Pass, cross-platform canonical in generation and checkout, and byte-stable. Swagger remains Development-only. |
| TypeScript client | CodeGenerator and generated Angular client | two normalized generations hash `0d77424cd59efd63cbb8ea0ae05a20684a79a1da59c110c8cfdfe4293a468e90`; strict `tsc` | Pass. Stale DTO schemas were replaced by canonical views/root. The local Node patch warning is recorded in `decisions.md`. |
| Aspire/bootstrap/start workflow | AppHost, ServiceDefaults, bootstrap, `start.sh`, `.gitattributes` | doctor 5 pass/0 fail; Aspire restore; first/restart disposable bootstrap; live suites; AppHost tests | Pass for static and task-owned bootstrap paths. Full Aspire run against untrusted external `.env` and Azure publish were deliberately not performed. |
| Packages/locks/licenses | central packages, locks, tool manifest | locked restore; doctor; package check/audit | Pass. Audit exit 0, no vulnerability/deprecation findings; 14 update-only PE7008 warnings remain backlog. Identity Web and Swashbuckle metadata declares MIT. |
| Diagnostics and release hygiene | Problem Details, tracing/health tests, stale/secret scans, generators | full build/tests, `git diff --check`, tracked-artifact and obsolete-symbol scans | Pass. No tracked secrets/machine database artifacts or active legacy DTO/base/AzureAD UI references. |
| Documentation and skills | this folder, linked task records, canonical skills | skill and plugin validators | Pass: 11 skills and plugin metadata 1.1.0 validate. Guidance now requires LF shell verification and removal of unneeded transitional DTO surfaces. |

## Exact release evidence

- Release solution and SQL project builds: 0 warnings, 0 errors.
- Offline executable aggregate: 164 total, 123 succeeded, 0 failed, 41 expected live skips. Architecture 25/25; Database Integration 22 skipped; Bootstrap 5/5; Domain 37/37; Providers 19/19; WebApi 37 succeeded/19 live skipped.
- Final evidence SQL `beacon-ar-task5-evidence-de0118754268`: Database Integration 22/22 and WebApi 56/56, zero failed/skipped; first/restart bootstrap pass; cleanup containers/networks/images `0/0/0`. The sanitized per-test record is [live-sql-2026-08-02.md](evidence/live-sql-2026-08-02.md); raw TRX was deleted.
- Generation resource `beacon-ar-task5-601fd01e9c7e`: first and restart bootstrap pass; cleanup `0/0/0`.
- Paradigm doctor, package check/audit, validate, strict database validation, and focused Domain/Data/Providers/WebApi checks exit 0. Solution-wide checks have only the Aspire analyzer limitation recorded in `decisions.md`.
- `start.sh doctor` and Aspire restore exit 0; shell index/worktree are LF with `text eol=lf`.
- EFPT recovery/redaction fixtures exit 0. `npm ci` reports zero vulnerabilities and strict TypeScript compilation exits 0.
- Skill validator: 11 skills pass. Plugin validator: metadata 1.1.0 passes.

## Related task records

- [Foundation](../beacon-ar-foundation/)
- [Master data](../beacon-ar-master-data/)
- [Sales workflows](../beacon-ar-sales-workflows/)
- [API contract](../beacon-ar-api-contract/)
- [Database views](../beacon-ar-database-views/)
- [EF Power Tools](../beacon-ar-ef-power-tools/)
- [Framework CRUD](../beacon-ar-framework-crud/)
- [Web API](../beacon-ar-web-api/)
- [Prior final verification](../beacon-ar-final-verification/)
