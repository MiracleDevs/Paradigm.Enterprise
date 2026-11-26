#requires -Version 7.0
[CmdletBinding()]
param([Parameter(Mandatory)][string]$TemplatePackage)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repositoryRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
$sourcePackage = (Resolve-Path -LiteralPath $TemplatePackage).Path
$fixtureParent = [IO.Directory]::CreateDirectory((Join-Path $repositoryRoot 'artifacts/template-installation-tests')).FullName
$fixtureRoot = [IO.Directory]::CreateDirectory((Join-Path $fixtureParent ([guid]::NewGuid().ToString('N')))).FullName
$checkout = Join-Path $fixtureRoot 'local checkout'
$hive = Join-Path $fixtureRoot 'isolated hive'
$packageId = 'Paradigm.Enterprise.Templates.WebApi'
$installer = Join-Path $checkout 'templates/web-api/build/install-template.sh'
$releasePath = Join-Path $checkout 'build/Paradigm.Version.props'
$packageDirectory = [IO.Directory]::CreateDirectory((Join-Path $checkout 'templates/web-api/artifacts/packages')).FullName
$bash = if ($IsWindows -and (Test-Path -LiteralPath 'C:/Program Files/Git/bin/bash.exe')) {
    'C:/Program Files/Git/bin/bash.exe'
} else { (Get-Command bash -ErrorAction Stop).Source }
$completed = $false

function Invoke-DotNet([string[]]$CommandArguments) {
    & dotnet @CommandArguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($CommandArguments -join ' ') failed with exit code $LASTEXITCODE." }
}

function Invoke-Installer {
    & $bash $installer.Replace('\', '/') --hive $hive.Replace('\', '/')
    if ($LASTEXITCODE -ne 0) { throw "Local template installation failed with exit code $LASTEXITCODE." }
}

function Set-LocalPackage([string]$Version, [string]$Marker) {
    # Private fixture versions exercise replacement without publishing anything or
    # changing the framework dependencies inside the generated application.
    [xml]$release = Get-Content -LiteralPath $releasePath -Raw
    $release.Project.PropertyGroup.ParadigmEnterpriseVersion = $Version
    $release.Save($releasePath)
    $destination = Join-Path $packageDirectory "$packageId.$Version.nupkg"
    Copy-Item -LiteralPath $sourcePackage -Destination $destination -Force
    $archive = [IO.Compression.ZipFile]::Open($destination, [IO.Compression.ZipArchiveMode]::Update)
    try {
        $entry = @($archive.Entries | Where-Object { $_.FullName.EndsWith('.nuspec') })
        if ($entry.Count -ne 1) { throw 'Expected one template package manifest.' }
        $reader = [IO.StreamReader]::new($entry[0].Open())
        try { [xml]$spec = $reader.ReadToEnd() } finally { $reader.Dispose() }
        if ($spec.package.metadata.id -cne $packageId) { throw "Expected package $packageId." }
        $spec.package.metadata.version = $Version
        $entryName = $entry[0].FullName
        $entry[0].Delete()
        $stream = $archive.CreateEntry($entryName).Open()
        try { $spec.Save($stream) } finally { $stream.Dispose() }
        $writer = [IO.StreamWriter]::new($archive.CreateEntry('content/installation-test.txt').Open())
        try { $writer.Write($Marker) } finally { $writer.Dispose() }
    } finally { $archive.Dispose() }
}

function Assert-Generation([string]$Scenario, [string]$ExpectedMarker) {
    $output = Join-Path $fixtureRoot $Scenario
    Invoke-DotNet @('new', '--debug:custom-hive', $hive, 'paradigm-webapi', '--name', 'InstallationValidation.Application', '--output', $output, '--no-update-check')
    $marker = Get-Content -LiteralPath (Join-Path $output 'installation-test.txt') -Raw
    if ($marker -cne $ExpectedMarker) { throw "$Scenario generated stale content: '$marker'." }
    if (-not (Test-Path -LiteralPath (Join-Path $output 'InstallationValidation.Application.slnx'))) {
        throw "$Scenario did not generate the solution."
    }
    Write-Output "PASS: $Scenario generated the expected package content."
}

try {
    foreach ($relative in @('templates/web-api/build/install-template.sh', 'templates/web-api/Paradigm.Enterprise.Templates.WebApi.csproj', 'build/Paradigm.Version.props')) {
        $destination = Join-Path $checkout $relative
        $null = [IO.Directory]::CreateDirectory((Split-Path -Parent $destination))
        Copy-Item -LiteralPath (Join-Path $repositoryRoot $relative) -Destination $destination
    }
    [xml]$release = Get-Content -LiteralPath $releasePath -Raw
    $coreVersion = $release.Project.PropertyGroup.ParadigmEnterpriseVersion.Split('-')[0]
    $firstVersion = "$coreVersion-installation.1"
    $nextVersion = "$coreVersion-installation.2"

    Set-LocalPackage $firstVersion 'initial'
    Invoke-Installer
    Assert-Generation 'first-install' 'initial'
    Invoke-Installer
    Assert-Generation 'repeat-install' 'initial'

    Remove-Item -LiteralPath (Join-Path $packageDirectory "$packageId.$firstVersion.nupkg")
    & $bash $installer.Replace('\', '/') --hive $hive.Replace('\', '/')
    if ($LASTEXITCODE -eq 0) { throw 'Installation should fail when the local package is missing.' }
    Assert-Generation 'missing-package-preserves-install' 'initial'

    Set-LocalPackage $firstVersion 'rebuilt'
    Invoke-Installer
    Assert-Generation 'same-version-rebuild' 'rebuilt'
    Set-LocalPackage $nextVersion 'upgraded'
    Invoke-Installer
    Assert-Generation 'upgrade' 'upgraded'
    Set-LocalPackage $firstVersion 'downgraded'
    Invoke-Installer
    Assert-Generation 'downgrade' 'downgraded'

    Invoke-DotNet @('new', '--debug:custom-hive', $hive, 'uninstall', $packageId)
    $null = & dotnet new --debug:custom-hive $hive paradigm-webapi --name Removed.Application --output (Join-Path $fixtureRoot 'removed') --no-update-check 2>&1
    if ($LASTEXITCODE -eq 0 -or (Test-Path -LiteralPath (Join-Path $fixtureRoot 'removed'))) {
        throw 'Uninstalled template remains usable.'
    }
    Write-Output 'PASS: removal makes the template unavailable.'
    Invoke-Installer
    Assert-Generation 'install-after-removal' 'downgraded'
    Invoke-DotNet @('new', '--debug:custom-hive', $hive, 'uninstall', $packageId)
    $completed = $true
} finally {
    if ($completed) {
        $resolvedFixture = (Resolve-Path -LiteralPath $fixtureRoot).Path
        if (-not $resolvedFixture.StartsWith($fixtureParent + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
            ((Get-Item -LiteralPath $resolvedFixture).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Unsafe test fixture cleanup target: $resolvedFixture."
        }
        Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
    } else {
        Write-Output "Installation test work directory: $fixtureRoot"
    }
}
