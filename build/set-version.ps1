#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$')]
    [string]$Version,
    [string]$ExpectedVersion
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$releasePath = Join-Path $repositoryRoot 'build/Paradigm.Version.props'
$pluginPath = Join-Path $repositoryRoot '.codex-plugin/plugin.json'
$templatePath = Join-Path $repositoryRoot 'templates/web-api/src/Directory.Packages.props'
[xml]$release = Get-Content -LiteralPath $releasePath -Raw
$currentVersion = $release.Project.PropertyGroup.ParadigmEnterpriseVersion
if ($ExpectedVersion -and $currentVersion -cne $ExpectedVersion) {
    throw "Expected version '$ExpectedVersion', found '$currentVersion'."
}
if ($Version.Contains('-')) {
    foreach ($identifier in $Version.Split('-', 2)[1].Split('.')) {
        if ($identifier -match '^0[0-9]+$') { throw 'Numeric prerelease identifiers cannot have leading zeroes.' }
    }
}
$releaseHeading = 'Version `{0}`' -f $Version
if (@(Get-Content -LiteralPath (Join-Path $repositoryRoot 'CHANGELOG.md')) -cnotcontains $releaseHeading) {
    throw "Add the '$releaseHeading' changelog entry and release notes before setting the release version."
}
$releaseText = [IO.File]::ReadAllText($releasePath)
$pluginText = [IO.File]::ReadAllText($pluginPath)
if ([regex]::Matches($releaseText, '<ParadigmEnterpriseVersion>[^<]+</ParadigmEnterpriseVersion>').Count -ne 1 -or
    [regex]::Matches($pluginText, '(?m)^\s*"version"\s*:\s*"[^"]+"').Count -ne 1) {
    throw 'Expected one release version property and one plugin version field.'
}
$originalFiles = @{}
foreach ($path in $releasePath, $pluginPath, $templatePath) { $originalFiles[$path] = [IO.File]::ReadAllBytes($path) }
try {
    $updatedRelease = [regex]::Replace($releaseText, '(<ParadigmEnterpriseVersion>)[^<]+(</ParadigmEnterpriseVersion>)', '${1}' + $Version + '${2}')
    $updatedPlugin = [regex]::Replace($pluginText, '(?m)^(\s*"version"\s*:\s*")[^"]+(".*)$', '${1}' + $Version + '${2}')
    [IO.File]::WriteAllText($releasePath, $updatedRelease, [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText($pluginPath, $updatedPlugin, [Text.UTF8Encoding]::new($false))
    & (Join-Path $PSScriptRoot 'templates/update-templates.ps1')
    & (Join-Path $PSScriptRoot 'verify-version.ps1')
} catch {
    foreach ($path in $originalFiles.Keys) { [IO.File]::WriteAllBytes($path, $originalFiles[$path]) }
    throw
}
Write-Output "Release metadata and templates updated from $currentVersion to $Version."
