# Beacon AR context boundaries — live validation

## Scope

This secret-free audit record covers the second Task 3 remediation on 2026-08-03 America/Buenos_Aires (2026-08-04 UTC). Connection credentials were generated inside one PowerShell process, passed only through process/container environment, redacted below, and discarded during cleanup.

## Recovery boundary

The recovery state contains an exact manifest of 35 generated persistence files and 13 handwritten partials: all 12 `*.Behavior.cs` files plus `MasterDataDbContext.Relationships.cs`. Recognized output state contains generated-marker C# paths and the recognized EFPT readme byproduct when present.

Both fixtures execute `Invoke-RecoverableWorkflow`, the production backup/catch/recovery wrapper:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File build/regenerate-persistence.ps1 `
  -TestFailureRecovery -TestRedaction

powershell -NoProfile -ExecutionPolicy Bypass -File build/regenerate-persistence.ps1 `
  -ConnectionString '<redacted disposable SQL Server connection>' `
  -TestLiveFailureRecovery
```

- Fast fixture: passed after mutating one generated context, one protected behavior partial, one new generated-marker file, and the recognized EFPT readme.
- Live fixture: Access EFPT completed successfully; failure was then injected before MasterData; production recovery passed.
- Pre/post generated, protected, and recognized-output path/hash fingerprint for both fixtures: `A9A8494DB003348634A3DCFEDA0230D9BBAE2999A0CC9F3860C9B439DEB39DDE`.

## Governed SQL Server publication

Task-owned resources were labeled `paradigm.task=beacon-ar-context-boundaries-r2`:

- SQL container: `beacon-ar-task3-r2-sql`
- Bootstrap container: `beacon-ar-task3-r2-bootstrap-run`
- Bootstrap image: `beacon-ar-task3-r2-bootstrap`
- Database: `BeaconArTask3R2`
- Host port: `14339`
- SQL Server source image ID: `sha256:2b41d0be82839692f678a709e8b7dd6106ee4776b0e70759c59b067730058b04`
- Built bootstrap image ID before removal: `sha256:3cc1c6784986a20a98800b50d47a6e1983c087d0a677b1a6be3029db0b0023cc`

The audited command shape was:

```powershell
docker build --label paradigm.task=beacon-ar-context-boundaries-r2 `
  -f examples/beacon-ar/src/BeaconAr.DatabaseBootstrap/Dockerfile `
  -t beacon-ar-task3-r2-bootstrap .

docker run -d --name beacon-ar-task3-r2-sql `
  --label paradigm.task=beacon-ar-context-boundaries-r2 `
  -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='<ephemeral process-scoped secret>' `
  -p 14339:1433 mcr.microsoft.com/mssql/server:2022-latest

docker run --name beacon-ar-task3-r2-bootstrap-run `
  --label paradigm.task=beacon-ar-context-boundaries-r2 `
  -e ConnectionStrings__DatabaseConnection='<redacted>' `
  -e Database__Mode=Managed -e Database__PublishOnStart=true `
  beacon-ar-task3-r2-bootstrap
```

The finite bootstrap completed database wait, DACPAC verification, pre-pre-deployment, deploy report, publish, and schema probe. No persistent volume was attached to the Task 3 SQL container.

## Test results

Commands were run from `examples/beacon-ar` so its Microsoft Testing Platform SDK configuration applied:

```powershell
dotnet test tests/BeaconAr.Architecture.Tests/BeaconAr.Architecture.Tests.csproj `
  --configuration Release --no-restore

$env:ConnectionStrings__DatabaseConnection = '<redacted disposable SQL Server connection>'

dotnet test tests/BeaconAr.Database.IntegrationTests/BeaconAr.Database.IntegrationTests.csproj `
  --configuration Release --no-restore --filter 'TestCategory=Integration'

dotnet test tests/BeaconAr.WebApi.Tests/BeaconAr.WebApi.Tests.csproj `
  --configuration Release --no-restore --filter 'TestCategory=Integration'

dotnet build BeaconAr.slnx --configuration Release --no-restore

dotnet run --project ../../src/Paradigm.Enterprise.Cli/Paradigm.Enterprise.Cli.csproj -- `
  database validate --project src/database/BeaconAr.Database.sqlproj `
  --solution BeaconAr.slnx --strict
```

- Architecture: 40 passed, 0 failed, 0 skipped.
- Live database integration: 24 passed, 0 failed, 0 skipped.
- Live Web API integration: 19 passed, 0 failed, 0 skipped.
- Release build: passed with 0 warnings and 0 errors.
- Repository-source strict database validation: passed.

## Artifact hashes

- `build/regenerate-persistence.ps1`: `E0D8A71987F56F2957FF189879041058E7124D2A95CB170442E6BB36D284FEF7`
- Published source DACPAC `src/database/bin/Release/BeaconAr.Database.dacpac`: `992AA42CC75E744B2F32C7E9915E24C044D13C189A9E554801D98781E49C14AA`
- `routine-validation.sha256`: `55999B6DA39661464FEA512A350ACEFDB185D8B9CBA0D69AD5EF950A06724017`

## Cleanup and unrelated resource boundary

Cleanup selected resources only by the Task 3 remediation label, then removed the exact bootstrap image. The following checks returned no output:

```powershell
docker ps -a --filter 'label=paradigm.task=beacon-ar-context-boundaries-r2' `
  --format '{{.Names}}|{{.Status}}|{{.Image}}'

docker images beacon-ar-task3-r2-bootstrap --format '{{.Repository}}:{{.Tag}}|{{.ID}}'
```

The existing `sqlserver-a91e0506` resource was not created by this remediation. Docker inspection shows it was created at `2026-08-02T15:10:32.044871825Z`, carries `com.microsoft.developer.usvc-dev.persistent=true` and `com.microsoft.developer.usvc-dev.name=sqlserver-a91e0506`, and mounts the pre-existing persistent volume `beaconar.apphost-a91e05069e-sqlserver-data`. It remained untouched.

Two guarded `beacon-ar-efpt-backup-*` temp directories left by earlier Task 3 attempts were inspected by exact absolute path and removed. A final scan found no EFPT backup directory or recognized EFPT readme byproduct remaining.
