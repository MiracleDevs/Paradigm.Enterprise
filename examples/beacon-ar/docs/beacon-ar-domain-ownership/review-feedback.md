# Task 4 independent review feedback

## Decision

**Not approved.** The layer and generation migrations are broadly correct, but two domain correctness defects leave public entity operations capable of bypassing or crashing validation, and the current tests do not exercise the required failure paths.

## Findings

### High — Sales replacement is not an atomic public domain operation

- **Locations:** `src/BeaconAr.Domain/Sales/Entities/Quote.Behavior.cs:72`, especially assignments at lines 80-84; `src/BeaconAr.Domain/Sales/Entities/SalesOrder.Behavior.cs:110`, especially assignments at lines 118-122.
- **Observed behavior:** `PrepareReplacement` validates draft state, references, line changes, and failure-prone normalized fields, but returns only a line list. `ApplyReplacement` is a separate public method and neither proves that preparation occurred nor revalidates the complete proposed state. A caller can invoke it directly on a non-draft aggregate. It also assigns customer/address/date fields before `NormalizeNotes` or `NormalizeTracking` can throw, leaving the object partially mutated. The production Providers currently call the methods in the expected order, but that convention is not enforced by the aggregate.
- **Consequence:** The entity's public API can bypass lifecycle invariants and can dirty an EF-tracked aggregate after a rejected replacement. This directly violates the task's complete-proposed-state and mutation-atomicity acceptance criterion.
- **Required remedy:** Expose one intention-revealing replacement operation that validates the entire proposed header, references, and lines before any mutation, then applies the already-normalized state and returns/applies the prepared lines. Alternatively use an opaque prepared-state value that is the only input accepted by a non-public/fail-free apply step. Do not retain two independently callable public phases. Add direct tests proving that invalid notes/tracking and a non-draft replacement leave every header field, audit field, and line unchanged for both Quote and SalesOrder.

### Medium — Idempotency intrinsic validation is neither total nor null-safe

- **Location:** `src/BeaconAr.Domain/Operations/Entities/IdempotencyRequest.Behavior.cs:50-77`.
- **Observed behavior:** `ValidateEntity` dereferences `Operation`, `KeyHash`, and `RequestHash` at lines 54-56, so a default or malformed generated persistence instance throws `NullReferenceException` instead of a domain validation failure. The state-specific branches cover only `InProgress` and `Completed`; the seeded, active `Failed` state is accepted without any defined resource/response/completion shape or transition behavior.
- **Consequence:** The entity does not own validation for its complete possible state, and invalid materialized/mapped state can escape the domain error contract. The active enum/seed member has no governed invariant.
- **Required remedy:** Normalize/copy nullable generated inputs into a complete proposed state before evaluating lengths, and define the reviewed `Failed`-state contract and transition (or explicitly reject that state until its semantics are designed). Cover default/null arrays and strings, every catalog state, invalid transitions, timestamp ordering, response bounds, and failure atomicity in direct Domain tests. Record the chosen failed-state semantics in `decisions.md` because the current framework/example does not define them.

### Medium — The regression suite does not meet the plan's entity-boundary and atomicity coverage

- **Locations:** `tests/BeaconAr.Domain.Tests/MasterDataValidationTests.cs:33`; `tests/BeaconAr.Domain.Tests/SalesWorkflowTests.cs:59`; `tests/BeaconAr.Domain.Tests/AccessAndOperationsBehaviorTests.cs:18-76`.
- **Observed behavior:** Only Product has a direct failed `Replace` atomicity test. The Sales test calls only `PrepareReplacement`, so it cannot detect the public `ApplyReplacement` partial-mutation/bypass defect. There are no equivalent failed replacement tests for Customer, CustomerAddress, Carrier, or SalesOrder, and no direct `Validate` null-state/failed-state coverage for IdempotencyRequest or AuditLog.
- **Consequence:** The acceptance statement that direct Domain tests cover every valid/invalid boundary and failed replacement/map atomicity is not evidenced; the highest-risk defect above passed all offline tests.
- **Required remedy:** Add focused tests for each changed entity's normalization and invalid boundary, every existing-object mutator's unchanged-on-failure guarantee, generated `MapFrom` prevalidation, and the Operations state shapes. Prefer state snapshots or exhaustive property assertions so audit and concurrency fields cannot be dirtied unnoticed.

### Low — Status enum validity remains duplicated in Providers

- **Locations:** `src/BeaconAr.Providers/Sales/QuoteProvider.cs:115`; `src/BeaconAr.Providers/Sales/SalesOrderProvider.cs:115`.
- **Observed behavior:** Both Providers perform `Enum.IsDefined(request.Status)` immediately before the aggregate's transition method performs the same intrinsic status check.
- **Consequence:** The migration leaves two owners for one rule and can produce different error contracts when the two checks evolve independently.
- **Required remedy:** Give the request type only transport-shape validation if a field-keyed 400 contract is required, and leave transition validity/allowed edges exclusively in the aggregate. Remove the duplicate Provider rule and retain only collaborator-dependent carrier/reference checks there.

## Verified strengths

- The broad `MasterDataRequestValidator`, `SalesRequestValidator`, `SalesDomainValidation`, `SafeUrlValidator`, and `ValidationErrorBuilder` owners are removed; no replacement `*RequestValidator` or `*DomainValidation` class remains.
- Handwritten behavior partials are co-located under Access, MasterData, Operations, and Sales namespaces. No generated EF entity/view/context or T4 template appears in the branch diff.
- `AddressType`, `IdempotencyState`, `QuoteStatus`, and `SalesOrderStatus` are runtime types in `BeaconAr.Interfaces`, with explicit IDs and `EnumMember` codes. The parity test compares the complete sorted enum set to the complete SQL seed tuple set.
- Splitting `BeaconAr.InterfaceGenerator` from the runtime Interfaces assembly is correctly classified under `04.Tools`; Domain references Interfaces normally and the generator only as an analyzer with `ReferenceOutputAssembly="false"`. The Roslyn package references were moved unchanged rather than newly introduced.
- Quote and SalesOrder searches now use typed procedures returning `QuoteView` and `SalesOrderView`; parallel summary DTOs, search-row types, and their mappers are removed. Detail compatibility DTOs are composed from canonical header/line views, and `decisions.md` names both the external compatibility reason and removal trigger.
- JSON metadata, OpenAPI, and the generated TypeScript client are coherent with the canonical search-view contract in the passing offline suite.
- Guidance changes are general, concise, and accurately capture object-owned validation, complete enum/seed parity, and canonical-view exceptions without Beacon-specific allow-lists.

## Validation evidence

| Check | Reviewer result |
| --- | --- |
| `dotnet build src/database/BeaconAr.Database.sqlproj --configuration Release --no-restore` | Passed; 0 warnings/errors. |
| `dotnet build BeaconAr.slnx --configuration Release --no-restore` | Passed; 0 warnings/errors. |
| `dotnet test --solution BeaconAr.slnx --configuration Release --no-build --no-restore --minimum-expected-tests 1` | Passed: 148 succeeded, 42 skipped, 0 failed. Skips are the connection-dependent SQL/API suites. |
| Strict SQL Server validation | Passed with no diagnostics. |
| Domain `paradigm doctor` and `paradigm checks run` | Passed. |
| Whole-solution package consistency | Passed. |
| `git diff --check` | Passed. |
| Live SQL Server/Web API validation | Not run: `ConnectionStrings__DatabaseConnection` is absent and no disposable Beacon/SQL container is running. |

The lack of a live connection remains an environment gap, but it is not the reason for non-approval; the public-domain-operation defects are deterministically visible in source.

## Cycle 2 re-review - 2026-08-04

### Decision

**Approved.** Every cycle-1 finding is resolved, the fixes are covered at the appropriate Domain boundary, and no new correctness or architectural defect was found in the remediation diff.

### Finding resolution

- **Atomic Sales replacement:** Resolved. Quote and SalesOrder each expose one public `Replace` operation. Draft-state, reference, normalized notes/tracking, replacement-line, aggregate-total, audit, and complete proposed-header checks all execute before the first assignment. The Providers call this operation and stage only its returned validated lines. The former public `PrepareReplacement` and `ApplyReplacement` methods no longer exist.
- **Idempotency validation:** Resolved. `ProposedState` normalization converts null strings/arrays into validation-safe values. Validation exhaustively handles `InProgress`, `Completed`, and `Failed`; `Complete` and `Fail` build and validate terminal candidates before applying them, and both reject a second terminal transition without changing state. The chosen failed-state response/resource/audit contract is recorded in `decisions.md`.
- **Regression coverage:** Resolved. Exhaustive Quote and SalesOrder snapshots include header, snapshot, audit, deletion, row-version, and ordered line state across invalid notes/tracking and non-draft failures. MasterData now covers failed Product, Customer, CustomerAddress, and Carrier replacement. Operations tests cover null AuditLog/Idempotency values, all catalog states, invalid response/timestamp/resource shapes, and terminal transition atomicity.
- **Provider duplication:** Resolved. QuoteProvider and SalesOrderProvider no longer call `Enum.IsDefined` for transition status; allowed status values and lifecycle edges have one aggregate owner. Provider reference/carrier queries remain collaborator-dependent checks.

### Cycle 2 validation evidence

| Check | Result |
| --- | --- |
| Release solution build without restore | Passed; 0 warnings/errors. |
| Complete solution test run without build/restore | Passed: 156 succeeded, 42 connection-dependent cases skipped, 0 failed. |
| Strict SQL Server source validation | Passed with no diagnostics. |
| `git diff --check` | Passed. |
| Source ownership scan | No production `PrepareReplacement`/`ApplyReplacement` remnants and no Provider transition-status `Enum.IsDefined` duplication. |

The previously recorded live SQL Server/Web API and two-pass EF Core Power Tools regeneration gap remains because `ConnectionStrings__DatabaseConnection` is absent. It does not prevent approval of the deterministic final-fix pass; the connection-dependent validation should still be run in the final environment before release.
