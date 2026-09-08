param(
    [int]$Port = 51793
)

$ErrorActionPreference = 'Stop'

$exampleRoot = Split-Path -Parent $PSScriptRoot
$projectDirectory = Join-Path $exampleRoot 'src/BeaconAr.WebApi'
$applicationPath = Join-Path $projectDirectory 'bin/Release/net10.0/BeaconAr.WebApi.exe'
$artifactPath = Join-Path $exampleRoot 'artifacts/openapi/beacon-ar-v1.json'
$documentUri = "http://127.0.0.1:$Port/openapi/v1.json"

& dotnet build (Join-Path $projectDirectory 'BeaconAr.WebApi.csproj') -c Release --no-restore
if ($LASTEXITCODE -ne 0) {
    throw "BeaconAr.WebApi failed to build with exit code $LASTEXITCODE."
}

$startInfo = [System.Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = $applicationPath
$startInfo.WorkingDirectory = $projectDirectory
$startInfo.UseShellExecute = $false
$startInfo.CreateNoWindow = $true
$startInfo.RedirectStandardOutput = $true
$startInfo.RedirectStandardError = $true
$startInfo.Environment['ASPNETCORE_ENVIRONMENT'] = 'Development'
$startInfo.Environment['ASPNETCORE_URLS'] = "http://127.0.0.1:$Port"
$startInfo.Environment['ConnectionStrings__DatabaseConnection'] = 'Server=(local);Database=BeaconArOpenApi;Integrated Security=true;TrustServerCertificate=true'
$startInfo.Environment['AzureAd__Instance'] = 'https://login.microsoftonline.com/'
$startInfo.Environment['AzureAd__TenantId'] = 'openapi-generation'
$startInfo.Environment['AzureAd__ClientId'] = 'api://beacon-openapi-generation'
$startInfo.Environment['AzureAd__AllowWebApiToBeAuthorizedByACL'] = 'true'
$startInfo.Environment['Cors__AllowedOrigins__0'] = 'https://localhost:4200'

$process = [System.Diagnostics.Process]::Start($startInfo)
if ($null -eq $process) {
    throw 'BeaconAr.WebApi could not be started.'
}

try {
    $content = $null
    $deadline = [DateTimeOffset]::UtcNow.AddSeconds(30)
    while ([DateTimeOffset]::UtcNow -lt $deadline -and $null -eq $content) {
        if ($process.HasExited) {
            $standardError = $process.StandardError.ReadToEnd()
            throw "BeaconAr.WebApi exited before serving OpenAPI. $standardError"
        }

        try {
            $response = Invoke-WebRequest -Uri $documentUri -UseBasicParsing -TimeoutSec 2
            if ($response.StatusCode -eq 200) {
                $content = $response.Content
            }
        }
        catch {
            Start-Sleep -Milliseconds 200
        }
    }

    if ($null -eq $content) {
        throw "OpenAPI document was not available at $documentUri within 30 seconds."
    }

    $null = $content | ConvertFrom-Json
    $artifactDirectory = Split-Path -Parent $artifactPath
    [System.IO.Directory]::CreateDirectory($artifactDirectory) | Out-Null
    $normalizedContent = $content.Replace("`r`n", "`n").Replace("`r", "`n").TrimEnd() + "`n"
    [System.IO.File]::WriteAllText(
        $artifactPath,
        $normalizedContent,
        [System.Text.UTF8Encoding]::new($false))
    Write-Output "Generated $artifactPath"
}
finally {
    if (!$process.HasExited) {
        $process.Kill()
        $process.WaitForExit()
    }

    $process.Dispose()
}
