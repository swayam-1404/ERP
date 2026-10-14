$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Import-LocalEnvironment.ps1')
Set-Location $projectRoot
dotnet run --project backend/src/VHEI.Api --no-launch-profile --no-build
