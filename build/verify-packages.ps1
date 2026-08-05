param(
    [Parameter(Mandatory = $true)]
    [string]$ArtifactsDirectory
)

$ErrorActionPreference = "Stop"
$resolvedArtifacts = (Resolve-Path -LiteralPath $ArtifactsDirectory).Path
$packages = @(Get-ChildItem -LiteralPath $resolvedArtifacts -Filter "Paradigm.Enterprise.*.nupkg" -File | Where-Object { -not $_.Name.EndsWith(".snupkg", [StringComparison]::OrdinalIgnoreCase) })

if ($packages.Count -eq 0) {
    throw "No Paradigm.Enterprise NuGet packages were found in '$resolvedArtifacts'."
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$errors = @()

foreach ($package in $packages) {
    $archive = [System.IO.Compression.ZipFile]::OpenRead($package.FullName)
    try {
        $nuspecEntry = $archive.Entries | Where-Object { $_.FullName.EndsWith(".nuspec", [StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1
        if ($null -eq $nuspecEntry) {
            $errors += "$($package.Name): package has no nuspec."
            continue
        }

        $reader = [System.IO.StreamReader]::new($nuspecEntry.Open())
        try {
            [xml]$nuspec = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }

        $readmeNode = $nuspec.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']/*[local-name()='readme']")
        $readmePath = if ($null -eq $readmeNode) { "" } else { $readmeNode.InnerText.Trim().TrimStart("/", "\") }
        if ([string]::IsNullOrWhiteSpace($readmePath)) {
            $errors += "$($package.Name): nuspec does not declare a readme."
            continue
        }

        $readmeEntry = $archive.Entries | Where-Object { $_.FullName.Equals($readmePath, [StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1
        if ($null -eq $readmeEntry -or $readmeEntry.Length -eq 0) {
            $errors += "$($package.Name): declared readme '$readmePath' is missing or empty."
        }
    }
    finally {
        $archive.Dispose()
    }
}

if ($errors.Count -gt 0) {
    throw ($errors -join [Environment]::NewLine)
}

Write-Output "Validated package readmes in $($packages.Count) Paradigm.Enterprise packages."
