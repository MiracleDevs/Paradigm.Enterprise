#requires -Version 7.0
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repositoryRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
$fixtureParent = [IO.Directory]::CreateDirectory((Join-Path $repositoryRoot 'artifacts/template-maintenance-tests')).FullName
$fixtureRoot = [IO.Directory]::CreateDirectory((Join-Path $fixtureParent ([guid]::NewGuid().ToString('N')))).FullName

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}
function Assert-Failure([scriptblock]$Action, [string]$ExpectedMessage) {
    $failure = $null
    try { $null = & $Action } catch { $failure = $_.Exception.Message }
    Assert-True ($null -ne $failure -and $failure -match $ExpectedMessage) "Expected failure matching '$ExpectedMessage', received '$failure'."
}

try {
    $relativeFiles = @(
        'build/templates/update-templates.ps1', 'build/set-version.ps1', 'build/verify-version.ps1',
        'build/Paradigm.Version.props', 'src/Directory.Packages.props',
        '.codex-plugin/plugin.json', 'templates/web-api/src/Directory.Packages.props'
    )
    $sourceRoot = Join-Path $repositoryRoot 'templates/web-api/src'
    foreach ($project in Get-ChildItem -LiteralPath $sourceRoot -Recurse -Filter '*.csproj' -File) {
        if ($project.FullName -match '[\\/](bin|obj)[\\/]') { continue }
        $relativeFiles += [IO.Path]::GetRelativePath($repositoryRoot, $project.FullName)
    }
    foreach ($relative in $relativeFiles) {
        $destination = Join-Path $fixtureRoot $relative
        $null = [IO.Directory]::CreateDirectory((Split-Path -Parent $destination))
        Copy-Item -LiteralPath (Join-Path $repositoryRoot $relative) -Destination $destination
    }
    $null = [IO.Directory]::CreateDirectory((Join-Path $fixtureRoot 'example'))
    $releasePath = Join-Path $fixtureRoot 'build/Paradigm.Version.props'
    $centralPath = Join-Path $fixtureRoot 'templates/web-api/src/Directory.Packages.props'
    $frameworkCentralPath = Join-Path $fixtureRoot 'src/Directory.Packages.props'
    $updateScript = Join-Path $fixtureRoot 'build/templates/update-templates.ps1'
    $setScript = Join-Path $fixtureRoot 'build/set-version.ps1'
    [xml]$release = Get-Content -LiteralPath $releasePath -Raw
    $oldVersion = $release.Project.PropertyGroup.ParadigmEnterpriseVersion
    $coreVersion = [version]$oldVersion.Split('-')[0]
    $newVersion = '{0}.{1}.{2}-preview.1' -f $coreVersion.Major, $coreVersion.Minor, ($coreVersion.Build + 1)
    $releaseNotes = "# Changelog`n`n" + ('Version `{0}`' -f $newVersion) + "`n`n- Fixture release.`n"
    [IO.File]::WriteAllText((Join-Path $fixtureRoot 'CHANGELOG.md'), $releaseNotes)

    [xml]$central = Get-Content -LiteralPath $centralPath -Raw
    [xml]$frameworkCentral = Get-Content -LiteralPath $frameworkCentralPath -Raw
    $shared = @($central.SelectNodes('//PackageVersion') | Where-Object {
        $frameworkCentral.SelectSingleNode("//PackageVersion[@Include='$($_.GetAttribute('Include'))']")
    })[0]
    $sharedId = $shared.GetAttribute('Include')
    $templateOnly = @($central.SelectNodes('//PackageVersion') | Where-Object {
        -not $_.GetAttribute('Include').StartsWith('Paradigm.Enterprise.') -and
        -not $frameworkCentral.SelectSingleNode("//PackageVersion[@Include='$($_.GetAttribute('Include'))']")
    })[0]
    $templateOnlyId = $templateOnly.GetAttribute('Include')
    $templateOnlyVersion = $templateOnly.GetAttribute('Version')
    $frameworkCentral.SelectSingleNode("//PackageVersion[@Include='$sharedId']").SetAttribute('Version', '99.1.0')
    $frameworkCentral.Save($frameworkCentralPath)

    $beforeCheck = (Get-FileHash -LiteralPath $centralPath).Hash
    Assert-Failure { & $updateScript -Check } 'Template version drift'
    Assert-True ((Get-FileHash -LiteralPath $centralPath).Hash -ceq $beforeCheck) 'Check mode changed the central file.'
    $null = & $updateScript
    [xml]$updated = Get-Content -LiteralPath $centralPath -Raw
    Assert-True ($updated.SelectSingleNode("//PackageVersion[@Include='$sharedId']").GetAttribute('Version') -ceq '99.1.0') 'Shared dependency did not synchronize.'
    Assert-True ($updated.SelectSingleNode("//PackageVersion[@Include='$templateOnlyId']").GetAttribute('Version') -ceq $templateOnlyVersion) 'A template-only dependency changed.'
    $afterUpdate = (Get-FileHash -LiteralPath $centralPath).Hash
    $null = & $updateScript
    Assert-True ((Get-FileHash -LiteralPath $centralPath).Hash -ceq $afterUpdate) 'Synchronization is not byte-stable on its second run.'
    Write-Output 'PASS: read-only drift detection, shared dependency synchronization, template-only pins, and idempotence.'

    $null = & $setScript -Version $newVersion -ExpectedVersion $oldVersion
    [xml]$updatedRelease = Get-Content -LiteralPath $releasePath -Raw
    [xml]$updated = Get-Content -LiteralPath $centralPath -Raw
    $plugin = Get-Content -LiteralPath (Join-Path $fixtureRoot '.codex-plugin/plugin.json') -Raw | ConvertFrom-Json
    Assert-True ($updatedRelease.Project.PropertyGroup.ParadigmEnterpriseVersion -ceq $newVersion) 'Release version did not update.'
    Assert-True ($updated.Project.PropertyGroup.ParadigmEnterpriseVersion -ceq $newVersion) 'Template release version did not update.'
    Assert-True ($plugin.version -ceq $newVersion) 'Plugin version did not update.'
    Write-Output 'PASS: prerelease bump aligns framework, plugin, and template metadata.'

    $projectPath = Join-Path $fixtureRoot ($relativeFiles | Where-Object { $_.EndsWith('.csproj') } | Select-Object -First 1)
    $projectBytes = [IO.File]::ReadAllBytes($projectPath)
    [xml]$invalidProject = Get-Content -LiteralPath $projectPath -Raw
    $invalidProject.SelectSingleNode('//PackageReference').SetAttribute('VersionOverride', '0.0.1')
    $invalidProject.Save($projectPath)
    Assert-Failure { & $updateScript -Check } 'must be versionless'
    $metadataPaths = @($releasePath, $centralPath, (Join-Path $fixtureRoot '.codex-plugin/plugin.json'))
    $metadataHashes = @($metadataPaths | ForEach-Object { (Get-FileHash -LiteralPath $_).Hash })
    Assert-Failure { & $setScript -Version $oldVersion -ExpectedVersion $newVersion } 'changelog entry'
    Assert-True (@(Compare-Object $metadataHashes @($metadataPaths | ForEach-Object { (Get-FileHash -LiteralPath $_).Hash })).Count -eq 0) 'Failed release preflight changed metadata.'
    $rollbackVersion = $newVersion.Replace('-preview.1', '-preview.2')
    Add-Content -LiteralPath (Join-Path $fixtureRoot 'CHANGELOG.md') -Value ('Version `{0}`' -f $rollbackVersion)
    Assert-Failure { & $setScript -Version $rollbackVersion -ExpectedVersion $newVersion } 'must be versionless'
    Assert-True (@(Compare-Object $metadataHashes @($metadataPaths | ForEach-Object { (Get-FileHash -LiteralPath $_).Hash })).Count -eq 0) 'Failed synchronization left a partial version bump.'
    [IO.File]::WriteAllBytes($projectPath, $projectBytes)
    $null = & $updateScript -Check
    Write-Output 'PASS: per-project version overrides are rejected; failed release preparation preserves all original metadata.'

    foreach ($packagingProperty in 'IsPackable', 'GeneratePackageOnBuild') {
        [xml]$packableProject = Get-Content -LiteralPath $projectPath -Raw
        $packableProject.SelectSingleNode("/Project/PropertyGroup/$packagingProperty").InnerText = 'true'
        $packableProject.Save($projectPath)
        Assert-Failure { & $updateScript -Check } 'must disable NuGet packaging'
        [IO.File]::WriteAllBytes($projectPath, $projectBytes)
    }
    Write-Output 'PASS: enabling application packing or package-on-build is rejected.'

    foreach ($relative in @(
        'templates/web-api/Paradigm.Enterprise.Templates.WebApi.csproj',
        'templates/web-api/README.md', 'templates/web-api/LICENSE',
        'templates/web-api/src/.template.config/icon.png',
        'templates/web-api/src/.template.config/template.json'
    )) {
        $destination = Join-Path $fixtureRoot $relative
        $null = [IO.Directory]::CreateDirectory((Split-Path -Parent $destination))
        Copy-Item -LiteralPath (Join-Path $repositoryRoot $relative) -Destination $destination
    }
    [xml]$driftedCentral = Get-Content -LiteralPath $centralPath -Raw
    $driftedCentral.Project.PropertyGroup.ParadigmEnterpriseVersion = '0.0.0'
    $driftedCentral.Save($centralPath)
    $packOutputDirectory = Join-Path $fixtureRoot 'package-output'
    $startInfo = [Diagnostics.ProcessStartInfo]::new((Get-Command dotnet).Source)
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    foreach ($argument in @('pack', (Join-Path $fixtureRoot 'templates/web-api/Paradigm.Enterprise.Templates.WebApi.csproj'), '--output', $packOutputDirectory, '--disable-build-servers')) {
        $startInfo.ArgumentList.Add($argument)
    }
    $packProcess = [Diagnostics.Process]::Start($startInfo)
    try {
        $packOutput = $packProcess.StandardOutput.ReadToEndAsync()
        $packError = $packProcess.StandardError.ReadToEndAsync()
        $packProcess.WaitForExit()
        $packLog = $packOutput.GetAwaiter().GetResult() + $packError.GetAwaiter().GetResult()
        Assert-True ($packProcess.ExitCode -ne 0 -and $packLog -match 'Template version drift') "Direct packing did not reject drift: $packLog"
    } finally { $packProcess.Dispose() }
    Assert-True (@(Get-ChildItem -LiteralPath $packOutputDirectory -Filter '*.nupkg' -ErrorAction SilentlyContinue).Count -eq 0) 'Packing wrote a package despite version drift.'
    Write-Output 'PASS: direct dotnet pack rejects stale central versions before producing a package.'
} finally {
    $resolvedFixture = (Resolve-Path -LiteralPath $fixtureRoot).Path
    if (-not $resolvedFixture.StartsWith($fixtureParent + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
        ((Get-Item -LiteralPath $resolvedFixture).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw "Unsafe test fixture cleanup target: $resolvedFixture."
    }
    Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
}
