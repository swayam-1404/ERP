$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Import-LocalEnvironment.ps1')
New-Item -ItemType Directory -Force -Path (Join-Path $projectRoot '.local') | Out-Null
& (Join-Path $PSScriptRoot 'Start-LocalPostgres.ps1')
$apiDll = Join-Path $projectRoot 'backend\src\VHEI.Api\bin\Debug\net10.0\VHEI.Api.dll'
if (-not (Test-Path -LiteralPath $apiDll)) { throw 'Build the backend first; see docs/deployment/LOCAL_SETUP.md.' }
if (-not (Get-NetTCPConnection -LocalPort 5100 -State Listen -ErrorAction SilentlyContinue)) {
    $apiProcess = Start-Process -FilePath (Get-Command dotnet).Source -ArgumentList @($apiDll) -WorkingDirectory (Split-Path $apiDll -Parent) -WindowStyle Hidden -RedirectStandardOutput (Join-Path $projectRoot '.local\api.log') -RedirectStandardError (Join-Path $projectRoot '.local\api-error.log') -PassThru
    $apiProcess.Id | Set-Content -LiteralPath (Join-Path $projectRoot '.local\api.pid')
}
if (-not (Get-NetTCPConnection -LocalPort 5173 -State Listen -ErrorAction SilentlyContinue)) {
    $viteCli = Join-Path $projectRoot 'frontend\node_modules\vite\bin\vite.js'
    $webProcess = Start-Process -FilePath (Get-Command node).Source -ArgumentList @($viteCli,'--host','127.0.0.1','--port','5173','--strictPort') -WorkingDirectory (Join-Path $projectRoot 'frontend') -WindowStyle Hidden -RedirectStandardOutput (Join-Path $projectRoot '.local\web.log') -RedirectStandardError (Join-Path $projectRoot '.local\web-error.log') -PassThru
    $webProcess.Id | Set-Content -LiteralPath (Join-Path $projectRoot '.local\web.pid')
}
$apiReady = $false
for ($attempt = 0; $attempt -lt 30; $attempt++) {
    try { $status = Invoke-RestMethod -Uri 'http://127.0.0.1:5100/health/ready' -TimeoutSec 2; if ($status.status -eq 'ready') { $apiReady = $true; break } } catch { }
    Start-Sleep -Milliseconds 500
}
if (-not $apiReady) { throw 'API readiness failed; inspect .local/api.log and api-error.log. No ready state is reported.' }
Write-Host 'Development services ready. Open http://127.0.0.1:5173. Credentials: .local/access.md.'
Write-Host 'Logs: .local/api.log and .local/web.log. Readiness: http://127.0.0.1:5100/health/ready.'
