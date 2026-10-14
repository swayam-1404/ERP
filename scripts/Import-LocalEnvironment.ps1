$projectRoot = Split-Path $PSScriptRoot -Parent
$envFile = Join-Path $projectRoot '.env'
if (-not (Test-Path -LiteralPath $envFile)) { throw 'Create .env from .env.example and set unique passwords first.' }
foreach ($line in Get-Content -LiteralPath $envFile) {
    if ($line -match '^([A-Za-z_][A-Za-z0-9_]*)=(.*)$') { [Environment]::SetEnvironmentVariable($Matches[1], $Matches[2], 'Process') }
}
$localDotnet = Join-Path $projectRoot '.tools\dotnet\dotnet.exe'
if (Test-Path -LiteralPath $localDotnet) { $env:DOTNET_ROOT = Split-Path $localDotnet; $env:PATH = "$env:DOTNET_ROOT;$env:PATH" }
