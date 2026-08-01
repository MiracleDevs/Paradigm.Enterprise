param(
    [Parameter(Mandatory = $false)]
    [string] $ConnectionString = $env:ConnectionStrings__DatabaseConnection
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    throw 'ConnectionStrings__DatabaseConnection is required for database-first generation.'
}

$exampleRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $exampleRoot 'src/BeaconAr.sln'
$dataProject = Join-Path $exampleRoot 'src/BeaconAr.Data/BeaconAr.Data.csproj'
$contextDirectory = Join-Path $exampleRoot 'src/BeaconAr.Data/Receivables/Generated'
$entityDirectory = Join-Path $exampleRoot 'src/BeaconAr.Domain/Receivables/Generated'

foreach ($generatedDirectory in @($contextDirectory, $entityDirectory)) {
    $resolvedParent = (Resolve-Path -LiteralPath (Split-Path -Parent $generatedDirectory)).Path
    $resolvedTarget = [System.IO.Path]::GetFullPath($generatedDirectory)
    if (-not $resolvedTarget.StartsWith($resolvedParent + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Generated output path escapes its owned parent: $resolvedTarget"
    }
}

dotnet build $solution --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'The solution must build before replacing generated persistence output.' }

foreach ($generatedDirectory in @($contextDirectory, $entityDirectory)) {
    if (Test-Path -LiteralPath $generatedDirectory) {
        Remove-Item -LiteralPath $generatedDirectory -Recurse -Force
    }
}

dotnet tool run dotnet-ef dbcontext scaffold $ConnectionString Microsoft.EntityFrameworkCore.SqlServer `
    --project $dataProject `
    --startup-project $dataProject `
    --context ReceivablesDbContext `
    --context-dir Receivables/Generated `
    --output-dir ../BeaconAr.Domain/Receivables/Generated `
    --namespace BeaconAr.Domain.Receivables.Generated `
    --context-namespace BeaconAr.Data.Receivables `
    --no-onconfiguring `
    --no-build `
    --force `
    --use-database-names
if ($LASTEXITCODE -ne 0) { throw 'Database-first generation failed.' }

dotnet build $solution --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Generated persistence output does not compile.' }
