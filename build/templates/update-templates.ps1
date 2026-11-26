#requires -Version 7.0
[CmdletBinding()]
param([switch]$Check)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$templateRoot = Join-Path $repositoryRoot 'templates/web-api/src'
$centralPath = Join-Path $templateRoot 'Directory.Packages.props'

[xml]$releaseProperties = Get-Content -LiteralPath (Join-Path $repositoryRoot 'build/Paradigm.Version.props') -Raw
$releaseVersions = $releaseProperties.SelectNodes('/Project/PropertyGroup/ParadigmEnterpriseVersion')
if ($releaseVersions.Count -ne 1 -or [string]::IsNullOrWhiteSpace($releaseVersions[0].InnerText)) {
    throw 'Expected one ParadigmEnterpriseVersion in build/Paradigm.Version.props.'
}
$releaseVersion = $releaseVersions[0].InnerText

function Get-CentralEntries([xml]$Document, [string]$Description) {
    $entries = [Collections.Generic.Dictionary[string, System.Xml.XmlElement]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in $Document.SelectNodes('/Project/ItemGroup/PackageVersion')) {
        $id = $entry.GetAttribute('Include')
        if ([string]::IsNullOrWhiteSpace($id) -or -not $entries.TryAdd($id, $entry)) {
            throw "$Description must declare each PackageVersion Include exactly once: '$id'."
        }
    }
    return ,$entries
}

$centralText = [IO.File]::ReadAllText($centralPath)
$central = [xml]::new()
$central.PreserveWhitespace = $true
$central.LoadXml($centralText)
$enabled = $central.SelectNodes('/Project/PropertyGroup/ManagePackageVersionsCentrally')
$templateVersions = $central.SelectNodes('/Project/PropertyGroup/ParadigmEnterpriseVersion')
if ($enabled.Count -ne 1 -or $enabled[0].InnerText -cne 'true' -or $templateVersions.Count -ne 1) {
    throw 'Template CPM must enable ManagePackageVersionsCentrally and declare one ParadigmEnterpriseVersion.'
}
$templateEntries = Get-CentralEntries $central 'Template CPM'
[xml]$frameworkCentral = Get-Content -LiteralPath (Join-Path $repositoryRoot 'src/Directory.Packages.props') -Raw
$frameworkEntries = Get-CentralEntries $frameworkCentral 'Framework CPM'

$projects = @(Get-ChildItem -LiteralPath $templateRoot -Recurse -Filter '*.csproj' -File |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' })
if ($projects.Count -eq 0) { throw 'No template projects found.' }
foreach ($projectFile in $projects) {
    [xml]$project = Get-Content -LiteralPath $projectFile.FullName -Raw
    foreach ($reference in $project.SelectNodes('//PackageReference')) {
        $id = $reference.GetAttribute('Include')
        if ($reference.HasAttribute('Version') -or $reference.HasAttribute('VersionOverride') -or
            $reference.SelectSingleNode('Version | VersionOverride')) {
            throw "Template package references must be versionless: $($projectFile.FullName), $id."
        }
        if (-not $templateEntries.ContainsKey($id)) {
            throw "Template CPM is missing a central version for '$id'."
        }
    }
}
$centralFiles = @(Get-ChildItem -LiteralPath $templateRoot -Recurse -Filter 'Directory.Packages.props' -File |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' })
if ($centralFiles.Count -ne 1 -or $centralFiles[0].FullName -cne $centralPath) {
    throw 'The template must have exactly one solution-root Directory.Packages.props.'
}

$changes = [Collections.Generic.List[string]]::new()
if ($templateVersions[0].InnerText -cne $releaseVersion) {
    $changes.Add("ParadigmEnterpriseVersion: $($templateVersions[0].InnerText) -> $releaseVersion")
    $templateVersions[0].InnerText = $releaseVersion
}
foreach ($entry in $templateEntries.Values) {
    $id = $entry.GetAttribute('Include')
    if ($entry.HasAttribute('Condition') -or $entry.ParentNode.HasAttribute('Condition')) {
        throw "Template central versions must be unconditional: $id."
    }
    $expected = $null
    if ($id.StartsWith('Paradigm.Enterprise.', [StringComparison]::OrdinalIgnoreCase)) {
        $expected = '$(ParadigmEnterpriseVersion)'
    } elseif ($frameworkEntries.ContainsKey($id)) {
        $shared = $frameworkEntries[$id]
        $expected = $shared.GetAttribute('Version')
        if ($shared.HasAttribute('Condition') -or $shared.ParentNode.HasAttribute('Condition') -or
            [string]::IsNullOrWhiteSpace($expected) -or $expected.Contains('$(')) {
            throw "Shared package '$id' needs an unconditional, literal framework version before it can be synchronized."
        }
    }
    if ($null -ne $expected -and $entry.GetAttribute('Version') -cne $expected) {
        $changes.Add("${id}: $($entry.GetAttribute('Version')) -> $expected")
        $entry.SetAttribute('Version', $expected)
    }
}

if ($changes.Count -gt 0) {
    if ($Check) {
        throw "Template version drift. Run pwsh build/templates/update-templates.ps1.`n$($changes -join "`n")"
    }
    $updatedText = $central.OuterXml
    if ($centralText.Contains("`r`n")) { $updatedText = $updatedText.Replace("`r`n", "`n").Replace("`n", "`r`n") }
    [IO.File]::WriteAllText($centralPath, $updatedText, [Text.UTF8Encoding]::new($false))
    $changes | Write-Output
}
Write-Output "Template central versions are aligned with Paradigm $releaseVersion."
