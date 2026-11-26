#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$FrameworkPackages,
    [string]$PackageOutputDirectory,
    [switch]$KeepWorkDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$templateRoot = Join-Path $repositoryRoot 'templates/web-api'
$sourceRoot = Join-Path $templateRoot 'src'
$frameworkFeed = (Resolve-Path -LiteralPath $FrameworkPackages).Path
if (-not $PackageOutputDirectory) { $PackageOutputDirectory = Join-Path $templateRoot 'artifacts/packages' }
$packageOutput = [IO.Path]::GetFullPath($PackageOutputDirectory)
$workParent = [IO.Directory]::CreateDirectory((Join-Path $repositoryRoot 'artifacts/template-validation')).FullName
$workRoot = [IO.Directory]::CreateDirectory((Join-Path $workParent ([guid]::NewGuid().ToString('N')))).FullName
$completed = $false

function Invoke-DotNet([string[]]$CommandArguments) {
    & dotnet @CommandArguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($CommandArguments -join ' ') failed with exit code $LASTEXITCODE." }
}

function Get-SourceFiles([string]$Root) {
    # Dot directories such as .template.config are hidden on Unix. They are part
    # of the package payload and must also participate in copying and validation.
    Get-ChildItem -LiteralPath $Root -Recurse -File -Force | Where-Object {
        [IO.Path]::GetRelativePath($Root, $_.FullName) -notmatch '(^|[\\/])(bin|obj|\.vs)([\\/]|$)' -and
        $_.Extension -notin '.user', '.suo'
    }
}

function Assert-NoApplicationPackages([string]$Root) {
    $packages = @(Get-ChildItem -LiteralPath $Root -Recurse -File | Where-Object { $_.Extension -in '.nupkg', '.snupkg' })
    if ($packages.Count -gt 0) { throw "Application projects emitted NuGet packages: $($packages.FullName -join ', ')" }
}

try {
    & (Join-Path $PSScriptRoot 'update-templates.ps1') -Check
    [xml]$release = Get-Content -LiteralPath (Join-Path $repositoryRoot 'build/Paradigm.Version.props') -Raw
    $version = $release.Project.PropertyGroup.ParadigmEnterpriseVersion
    $metadata = Get-Content -LiteralPath (Join-Path $sourceRoot '.template.config/template.json') -Raw | ConvertFrom-Json
    [xml]$packageProject = Get-Content -LiteralPath (Join-Path $templateRoot 'Paradigm.Enterprise.Templates.WebApi.csproj') -Raw
    $packageId = $packageProject.Project.PropertyGroup.PackageId
    $packageStage = [IO.Directory]::CreateDirectory((Join-Path $workRoot 'package')).FullName
    $packageCache = Join-Path $workRoot 'packages'
    $hive = Join-Path $workRoot 'hive'
    $generatedRoot = Join-Path $workRoot 'generated/src/api'
    $applicationName = 'TemplateValidation.Application'

    # A fresh cache and source mapping force every Paradigm dependency to the
    # packages produced by this checkout, even when the version exists globally.
    $cacheOutput = & dotnet nuget locals global-packages --list --force-english-output
    if ($LASTEXITCODE -ne 0) { throw 'Cannot determine the global NuGet cache.' }
    $globalCache = ($cacheOutput | Where-Object { $_ -like 'global-packages:*' } | Select-Object -First 1) -replace '^global-packages:\s*', ''
    [xml]$nuget = '<configuration><packageSources><clear /></packageSources><packageSourceMapping /></configuration>'
    $sources = @(
        @{ Key = 'framework'; Value = $frameworkFeed; Pattern = 'Paradigm.Enterprise.*' },
        @{ Key = 'nuget.org'; Value = 'https://api.nuget.org/v3/index.json'; Pattern = '*' }
    )
    if ($globalCache -and (Test-Path -LiteralPath $globalCache -PathType Container)) {
        $sources += @{ Key = 'existing-cache'; Value = $globalCache; Pattern = '*' }
    }
    foreach ($source in $sources) {
        $sourceElement = $nuget.CreateElement('add')
        $sourceElement.SetAttribute('key', $source.Key)
        $sourceElement.SetAttribute('value', $source.Value)
        $null = $nuget.SelectSingleNode('/configuration/packageSources').AppendChild($sourceElement)
        $mapping = $nuget.CreateElement('packageSource')
        $mapping.SetAttribute('key', $source.Key)
        $pattern = $nuget.CreateElement('package')
        $pattern.SetAttribute('pattern', $source.Pattern)
        $null = $mapping.AppendChild($pattern)
        $null = $nuget.SelectSingleNode('/configuration/packageSourceMapping').AppendChild($mapping)
    }
    $nugetConfig = Join-Path $workRoot 'NuGet.Config'
    $nuget.Save($nugetConfig)

    $restoreArguments = @('--configfile', $nugetConfig, '--packages', $packageCache, '--disable-build-servers')
    $sourceSolution = Join-Path $sourceRoot $metadata.primaryOutputs[0].path
    # Build a byte-for-byte copy so temporary restore paths never enter the
    # maintainer's source obj directories or depend on files outside the template.
    $authoringRoot = Join-Path $workRoot 'authoring'
    foreach ($file in Get-SourceFiles $sourceRoot) {
        $destination = Join-Path $authoringRoot ([IO.Path]::GetRelativePath($sourceRoot, $file.FullName))
        $null = [IO.Directory]::CreateDirectory((Split-Path -Parent $destination))
        Copy-Item -LiteralPath $file.FullName -Destination $destination
    }
    $authoringSolution = Join-Path $authoringRoot $metadata.primaryOutputs[0].path
    Invoke-DotNet (@('restore', $authoringSolution) + $restoreArguments)
    Invoke-DotNet @('build', $authoringSolution, '--configuration', 'Release', '--no-restore', '--disable-build-servers', '--maxcpucount:1', '--warnaserror')
    Invoke-DotNet @('pack', $authoringSolution, '--configuration', 'Release', '--no-build', '--no-restore', '--disable-build-servers', '--maxcpucount:1')
    Assert-NoApplicationPackages $authoringRoot
    Invoke-DotNet @('pack', (Join-Path $templateRoot 'Paradigm.Enterprise.Templates.WebApi.csproj'), '--configuration', 'Release', '--output', $packageStage, '--disable-build-servers')
    $packageFile = Join-Path $packageStage "$packageId.$version.nupkg"

    $archive = [IO.Compression.ZipFile]::OpenRead($packageFile)
    try {
        $content = @($archive.Entries | Where-Object { $_.FullName.StartsWith('content/') -and -not $_.FullName.EndsWith('/') })
        $expectedFiles = @(Get-SourceFiles $sourceRoot)
        $expectedEntries = @($expectedFiles | ForEach-Object { 'content/' + [IO.Path]::GetRelativePath($sourceRoot, $_.FullName).Replace('\', '/') })
        $actualEntries = @($content | ForEach-Object { $_.FullName })
        $missingEntries = @($expectedEntries | Where-Object { $_ -cnotin $actualEntries })
        $unexpectedEntries = @($actualEntries | Where-Object { $_ -cnotin $expectedEntries })
        if ($content.Count -ne $expectedFiles.Count -or $missingEntries.Count -gt 0 -or $unexpectedEntries.Count -gt 0 -or $actualEntries -match '/(bin|obj|\.vs)/') {
            throw "Template package source inventory does not match the canonical source. Expected $($expectedFiles.Count) files, found $($content.Count). Missing: [$($missingEntries -join ', ')]. Unexpected: [$($unexpectedEntries -join ', ')]."
        }
        foreach ($file in $expectedFiles) {
            $entryName = 'content/' + [IO.Path]::GetRelativePath($sourceRoot, $file.FullName).Replace('\', '/')
            $entry = $archive.GetEntry($entryName)
            if (-not $entry) { throw "Template package is missing '$entryName'." }
            $stream = $entry.Open()
            $hasher = [Security.Cryptography.SHA256]::Create()
            try { $hash = [Convert]::ToHexString($hasher.ComputeHash($stream)) }
            finally { $hasher.Dispose(); $stream.Dispose() }
            if ($hash -cne (Get-FileHash -LiteralPath $file.FullName).Hash) { throw "Stale template content: $entryName." }
        }
        $specEntry = @($archive.Entries | Where-Object { $_.FullName.EndsWith('.nuspec') })
        if ($specEntry.Count -ne 1) { throw 'Expected one template package manifest.' }
        $reader = [IO.StreamReader]::new($specEntry[0].Open())
        try { [xml]$spec = $reader.ReadToEnd() } finally { $reader.Dispose() }
        if ($spec.package.metadata.id -cne $packageId -or $spec.package.metadata.version -cne $version -or
            $spec.package.metadata.packageTypes.packageType.name -cne 'Template') {
            throw 'Template package identity, version, or package type is incorrect.'
        }
    } finally { $archive.Dispose() }

    & (Join-Path $PSScriptRoot 'tests/template-installation.tests.ps1') -TemplatePackage $packageFile
    Invoke-DotNet @('new', '--debug:custom-hive', $hive, 'install', $packageFile)
    Invoke-DotNet @('new', '--debug:custom-hive', $hive, $metadata.shortName, '--name', $applicationName, '--output', $generatedRoot, '--dry-run')
    if (Test-Path -LiteralPath $generatedRoot) { throw 'Template dry run wrote the output directory.' }
    Invoke-DotNet @('new', '--debug:custom-hive', $hive, $metadata.shortName, '--name', $applicationName, '--output', $generatedRoot)

    $centralFiles = @(Get-ChildItem -LiteralPath $generatedRoot -Recurse -Filter 'Directory.Packages.props' -File)
    if ($centralFiles.Count -ne 1 -or $centralFiles[0].DirectoryName -cne $generatedRoot) {
        throw 'Generated solution must have exactly one root Directory.Packages.props.'
    }
    $generatedSolution = Join-Path $generatedRoot $metadata.primaryOutputs[0].path.Replace($metadata.sourceName, $applicationName)
    [xml]$sourceSolutionXml = Get-Content -LiteralPath $sourceSolution -Raw
    [xml]$generatedSolutionXml = Get-Content -LiteralPath $generatedSolution -Raw
    $projectPaths = @($generatedSolutionXml.SelectNodes('/Solution/Folder/Project') | ForEach-Object { $_.GetAttribute('Path') })
    if ($projectPaths.Count -ne $sourceSolutionXml.SelectNodes('/Solution/Folder/Project').Count -or
        @($projectPaths | Sort-Object -Unique).Count -ne $projectPaths.Count) {
        throw 'Generated solution project membership is incomplete or duplicated.'
    }
    if (-not $generatedSolutionXml.SelectSingleNode('/Solution/Folder[@Name="/00.SolutionItems/"]/File[@Path="Directory.Packages.props"]')) {
        throw 'Generated solution does not register the central package file in solution items.'
    }
    $expectedFolders = @('00.SolutionItems', '01.Shared', '02.Modules', '03.Hosts', '04.Tools', '05.Tests')
    $generatedFolders = @($generatedSolutionXml.SelectNodes('/Solution/Folder') | ForEach-Object { $_.GetAttribute('Name').Trim('/') })
    if (@(Compare-Object $expectedFolders $generatedFolders).Count -gt 0) { throw 'Generated solution folders do not match the canonical layout.' }
    foreach ($projectPath in $projectPaths) {
        [xml]$project = Get-Content -LiteralPath (Join-Path $generatedRoot $projectPath) -Raw
        if ($project.SelectNodes('//PackageReference[@Version or @VersionOverride or Version or VersionOverride]').Count -gt 0) {
            throw "Generated project has per-project package versions: $projectPath."
        }
        foreach ($propertyName in 'IsPackable', 'GeneratePackageOnBuild') {
            $properties = $project.SelectNodes("/Project/PropertyGroup/$propertyName")
            if ($properties.Count -ne 1 -or $properties[0].InnerText -cne 'false') {
                throw "Generated application must disable ${propertyName}: $projectPath."
            }
        }
    }
    Invoke-DotNet (@('restore', $generatedSolution) + $restoreArguments)
    Invoke-DotNet @('build', $generatedSolution, '--configuration', 'Release', '--no-restore', '--disable-build-servers', '--maxcpucount:1', '--warnaserror')
    Invoke-DotNet @('pack', $generatedSolution, '--configuration', 'Release', '--no-build', '--no-restore', '--disable-build-servers', '--maxcpucount:1')
    Assert-NoApplicationPackages $generatedRoot
    & (Join-Path $PSScriptRoot 'test-host.ps1') -SolutionDirectory $generatedRoot -ProjectName $applicationName

    $localPackages = @(Get-ChildItem -LiteralPath $frameworkFeed -Filter '*.nupkg' -File)
    $verifiedFrameworkIds = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($projectPath in $projectPaths) {
        $projectDirectory = Split-Path -Parent (Join-Path $generatedRoot $projectPath)
        $assets = Get-Content -LiteralPath (Join-Path $projectDirectory 'obj/project.assets.json') -Raw | ConvertFrom-Json
        if (-not $assets.project.restore.centralPackageVersionsManagementEnabled) { throw "CPM was not discovered: $projectPath." }
        foreach ($library in $assets.libraries.PSObject.Properties) {
            if ($library.Value.type -ne 'package') { continue }
            $id, $resolvedVersion = $library.Name.Split('/', 2)
            if (-not $id.StartsWith('Paradigm.Enterprise.', [StringComparison]::OrdinalIgnoreCase)) { continue }
            if ($resolvedVersion -cne $version) { throw "$id restored $resolvedVersion instead of $version." }
            if (-not $verifiedFrameworkIds.Add($id)) { continue }
            $localPackage = @($localPackages | Where-Object { $_.BaseName -ieq "$id.$version" })
            if ($localPackage.Count -ne 1) { throw "Expected one local $id $version package in $frameworkFeed." }
            $cachedPackage = Join-Path $packageCache ($id.ToLowerInvariant() + '/' + $resolvedVersion.ToLowerInvariant() + '/' + $library.Name.Replace('/', '.').ToLowerInvariant() + '.nupkg')
            if ((Get-FileHash -LiteralPath $localPackage[0].FullName).Hash -cne (Get-FileHash -LiteralPath $cachedPackage).Hash) {
                throw "$id did not restore the package built by this checkout."
            }
        }
    }
    if ($verifiedFrameworkIds.Count -eq 0) { throw 'No local framework packages were validated.' }
    $null = [IO.Directory]::CreateDirectory($packageOutput)
    Copy-Item -LiteralPath $packageFile -Destination (Join-Path $packageOutput ([IO.Path]::GetFileName($packageFile))) -Force
    $completed = $true
    Write-Output "Template $packageId $version validated: source build, package payload, installation lifecycle, native generation, CPM, generated build, no application NuGet packages, and $($verifiedFrameworkIds.Count) exact local framework packages."
    Write-Output "Validated package: $(Join-Path $packageOutput ([IO.Path]::GetFileName($packageFile)))"
} finally {
    if ($completed -and -not $KeepWorkDirectory) {
        $resolvedWorkRoot = (Resolve-Path -LiteralPath $workRoot).Path
        if (-not $resolvedWorkRoot.StartsWith($workParent + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
            ((Get-Item -LiteralPath $resolvedWorkRoot).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Unsafe template validation cleanup target: $resolvedWorkRoot."
        }
        Remove-Item -LiteralPath $resolvedWorkRoot -Recurse -Force
    } else {
        Write-Output "Template validation work directory: $workRoot"
    }
}
