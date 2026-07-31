$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
[xml]$buildProperties = Get-Content -Raw (Join-Path $repositoryRoot "build/Paradigm.Version.props")
$version = $buildProperties.Project.PropertyGroup.ParadigmEnterpriseVersion
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "ParadigmEnterpriseVersion is missing from build/Paradigm.Version.props."
}

$expectedText = @{
    (Join-Path $repositoryRoot ".codex-plugin/plugin.json") = "`"version`": `"$version`""
    (Join-Path $repositoryRoot "CHANGELOG.md") = "Version ``$version``"
}

foreach ($entry in $expectedText.GetEnumerator()) {
    $content = Get-Content -Raw -LiteralPath $entry.Key
    if (-not $content.Contains($entry.Value)) {
        throw "Version drift: '$($entry.Key)' does not contain '$($entry.Value)'."
    }
}

$projectVersions = Get-ChildItem (Join-Path $repositoryRoot "src") -Filter *.csproj -Recurse |
    ForEach-Object {
        [xml]$project = Get-Content -Raw -LiteralPath $_.FullName
        foreach ($element in $project.SelectNodes("//*[local-name()='Version']")) {
            [pscustomobject]@{ Path = $_.FullName; Value = $element.InnerText }
        }
    }
if ($projectVersions) {
    $details = $projectVersions | ForEach-Object { "$($_.Path): <Version>$($_.Value)</Version>" }
    throw "Version drift: source project files must inherit ParadigmEnterpriseVersion and cannot declare Version.`n$($details -join "`n")"
}

$exampleParadigmReferences = Get-ChildItem (Join-Path $repositoryRoot "example") -Filter *.csproj -Recurse |
    ForEach-Object {
        [xml]$project = Get-Content -Raw -LiteralPath $_.FullName
        foreach ($reference in $project.SelectNodes("//*[local-name()='PackageReference' and starts-with(@Include, 'Paradigm.Enterprise.')]")) {
            $referenceVersion = $reference.GetAttribute("Version")
            if ($referenceVersion -ne '$(ParadigmEnterpriseVersion)') {
                [pscustomobject]@{ Path = $_.FullName; Package = $reference.GetAttribute("Include"); Version = $referenceVersion }
            }
        }
    }
if ($exampleParadigmReferences) {
    $details = $exampleParadigmReferences |
        ForEach-Object { "$($_.Path): $($_.Package) uses '$($_.Version)'" }
    throw "Version drift: example Paradigm package references must use `$(ParadigmEnterpriseVersion).`n$($details -join "`n")"
}

Write-Output "Paradigm version metadata is aligned at $version."
