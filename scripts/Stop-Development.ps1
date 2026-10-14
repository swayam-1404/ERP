$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
foreach ($service in @('api','web')) {
    $servicePidFile = Join-Path $projectRoot ".local\$service.pid"
    if (-not (Test-Path -LiteralPath $servicePidFile)) { continue }
    $serviceProcessId = [int](Get-Content -LiteralPath $servicePidFile)
    $serviceProcess = Get-CimInstance Win32_Process -Filter "ProcessId=$serviceProcessId"
    if ($serviceProcess -and $serviceProcess.CommandLine.Contains($projectRoot)) { Stop-Process -Id $serviceProcessId }
    Remove-Item -LiteralPath $servicePidFile
}
$pgBin = Join-Path $projectRoot '.tools\pg-runtime\pgsql\bin\pg_ctl.exe'
$pgData = Join-Path $projectRoot '.local\postgres-data'
if (Test-Path -LiteralPath $pgBin) { & $pgBin -D $pgData -w -m fast stop }
Write-Host 'Tracked development services stopped. Database files preserved.'
