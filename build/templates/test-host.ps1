#requires -Version 7.0
param([Parameter(Mandatory)][string]$SolutionDirectory, [Parameter(Mandatory)][string]$ProjectName)
$ErrorActionPreference = 'Stop'
$hostDirectory = Join-Path $SolutionDirectory "$ProjectName.WebApi"
$hostAssembly = Join-Path $hostDirectory "bin/Release/net10.0/$ProjectName.WebApi.dll"
foreach ($environmentName in 'Development', 'Production') {
    $portReservation = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
    $portReservation.Start()
    $port = $portReservation.LocalEndpoint.Port
    $portReservation.Stop()
    $startInfo = [Diagnostics.ProcessStartInfo]::new((Get-Command dotnet).Source)
    $startInfo.ArgumentList.Add($hostAssembly)
    $startInfo.WorkingDirectory = $hostDirectory
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.Environment['ASPNETCORE_URLS'] = "http://127.0.0.1:$port"
    $startInfo.Environment['ASPNETCORE_ENVIRONMENT'] = $environmentName
    $startInfo.Environment['DOTNET_ENVIRONMENT'] = $environmentName
    $startInfo.Environment['ConnectionStrings__SampleConnection'] = 'Server=127.0.0.1,1;Database=TemplateValidation;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=1'
    $hostProcess = [Diagnostics.Process]::Start($startInfo)
    $standardOutput = $hostProcess.StandardOutput.ReadToEndAsync()
    $standardError = $hostProcess.StandardError.ReadToEndAsync()
    try {
        $ready = $false
        $deadline = [DateTime]::UtcNow.AddSeconds(30)
        while ([DateTime]::UtcNow -lt $deadline) {
            if ($hostProcess.HasExited) { throw "Host exited with code $($hostProcess.ExitCode)." }
            try {
                $null = Invoke-WebRequest -Uri "http://127.0.0.1:$port/" -TimeoutSec 2 -SkipHttpErrorCheck
                $ready = $true
                break
            } catch { Start-Sleep -Milliseconds 250 }
        }
        if (-not $ready) { throw 'Host did not start.' }
        if ($environmentName -eq 'Development') {
            $openapiResponse = Invoke-WebRequest -Uri "http://127.0.0.1:$port/openapi/v1.json" -TimeoutSec 5
            $swaggerResponse = Invoke-WebRequest -Uri "http://127.0.0.1:$port/swagger/v1/swagger.json" -TimeoutSec 5
            $document = $openapiResponse.Content | ConvertFrom-Json
            if ($openapiResponse.StatusCode -ne 200 -or $document.info.title -cne "$ProjectName API") { throw 'OpenAPI document failed.' }
            if ($openapiResponse.Headers.'Content-Type' -notlike 'application/json*') { throw 'OpenAPI response has an unexpected content type.' }
            if ($openapiResponse.Content -cne $swaggerResponse.Content) { throw 'OpenAPI and Swagger routes return different documents.' }
            $scalarResponse = Invoke-WebRequest -Uri "http://127.0.0.1:$port/scalar/v1" -TimeoutSec 5
            if ($scalarResponse.StatusCode -ne 200 -or $scalarResponse.Content -notmatch 'scalar' -or $scalarResponse.Content -notmatch '"url":\s*"/?openapi/(v1|\{documentName\})\.json"') { throw 'Scalar page or OpenAPI configuration failed.' }
            $oldUiResponse = Invoke-WebRequest -Uri "http://127.0.0.1:$port/swagger/index.html" -TimeoutSec 5 -SkipHttpErrorCheck
            if ($oldUiResponse.StatusCode -ne 404) { throw 'The old Swagger UI endpoint is still active.' }
            Write-Output 'Development checks passed: Scalar UI 200, OpenAPI 200 and identical to Swagger document, old Swagger UI 404.'
        } else {
            foreach ($documentationRoute in '/scalar', '/scalar/v1', '/openapi/v1.json', '/swagger/v1/swagger.json', '/swagger/index.html') {
                $response = Invoke-WebRequest -Uri "http://127.0.0.1:$port$documentationRoute" -TimeoutSec 5 -SkipHttpErrorCheck
                if ($response.StatusCode -ne 404) { throw "Documentation is exposed in Production: $documentationRoute" }
            }
            Write-Output 'Production checks passed: Scalar, OpenAPI, and Swagger documentation routes all return 404.'
        }
    } catch {
        if (-not $hostProcess.HasExited) { $hostProcess.Kill($true) }
        $hostProcess.WaitForExit()
        Write-Output $standardOutput.GetAwaiter().GetResult()
        Write-Output $standardError.GetAwaiter().GetResult()
        throw
    } finally {
        if (-not $hostProcess.HasExited) { $hostProcess.Kill($true) }
        $hostProcess.WaitForExit()
        $hostProcess.Dispose()
    }
}