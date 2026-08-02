# Beacon AR final verification implementation plan

## Objective

Correct the final merged Beacon AR README so its documented offline test command works with the repository-pinned .NET 10 Microsoft.Testing.Platform runner, then repeat the release-facing checks against the merged example. This task changes documentation only; it must not alter application, database, generated, test, or workflow behavior.

## Required documentation correction

In `examples/beacon-ar/README.md`, replace the legacy solution invocation:

```bash
dotnet test src/BeaconAr.sln --configuration Release --filter "TestCategory!=Integration"
```

with the supported MTP solution form:

```bash
dotnet test --solution src/BeaconAr.sln --configuration Release --no-build --no-restore --minimum-expected-tests 1
```

Keep the command in the existing Verification section. The explicit solution option avoids treating the solution path as a project under MTP, and the minimum-test guard prevents an empty discovery run from appearing successful. The database integration tests discover and self-skip when no connection is configured, which keeps every MTP test module non-empty while preserving an offline run. Do not rewrite unrelated README content.

## Final merged verification

Run from `examples/beacon-ar` after restoring prerequisites:

```powershell
dotnet build src/BeaconAr.sln --configuration Release
dotnet test --solution src/BeaconAr.sln --configuration Release --no-build --no-restore --minimum-expected-tests 1
dotnet tool run paradigm database validate --project src/database/BeaconAr.Database.sqlproj --solution src/BeaconAr.sln --strict --format json
dotnet build src/BeaconAr.WebApi/BeaconAr.WebApi.csproj --configuration Release --no-restore
dotnet run --project src/BeaconAr.CodeGenerator/BeaconAr.CodeGenerator.csproj --configuration Release --no-build -- openapi-typescript ../../artifacts/openapi/beacon-ar-v1.json ../../tests/BeaconAr.ClientContract/generated/beacon-ar-v1.ts
npm ci --prefix tests/BeaconAr.ClientContract
npm run check --prefix tests/BeaconAr.ClientContract
```

Acceptance requires a warning- and error-free Release build, a non-empty offline MTP run with no failures, zero strict database diagnostics, successful OpenAPI generation and strict TypeScript compilation, and no unexpected generated drift. Finish from the repository root with:

```powershell
git diff --check
git status --short
```

Inspect the reported paths and confirm the final change set contains only the planned README correction and task records; do not claim a clean worktree merely because unrelated pre-existing changes remain outside this task.
