param([Parameter(Mandatory=$true)][ValidateSet('RESET LOCAL DEMO')][string]$Confirmation)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Import-LocalEnvironment.ps1')
if ($env:ASPNETCORE_ENVIRONMENT -ne 'Development' -or $env:Seed__Mode -ne 'Demo') { throw 'Demo reset only runs in Development with Demo seed mode.' }
if ($env:ConnectionStrings__Erp -notmatch '^Host=127\.0\.0\.1;Port=54329;Database=vhei;Username=vhei;Password=[A-Za-z0-9]+$') { throw 'Reset target must be the exact project-local vhei database. Custom or remote targets are rejected.' }
if (Get-NetTCPConnection -LocalPort 5100 -State Listen -ErrorAction SilentlyContinue) { throw 'Stop the API before resetting demo operational records.' }
$pgCli = Join-Path $projectRoot '.tools\pg-runtime\pgsql\bin\psql.exe'
$env:PGPASSWORD = $env:POSTGRES_PASSWORD
try {
    & $pgCli -h 127.0.0.1 -p 54329 -U vhei -d vhei -v ON_ERROR_STOP=1 -c 'BEGIN; DO $$ BEGIN IF EXISTS(SELECT 1 FROM "Students" WHERE NOT "IsDemo") OR EXISTS(SELECT 1 FROM "Faculty" WHERE NOT "IsDemo") OR EXISTS(SELECT 1 FROM "Guardians" WHERE NOT "IsDemo") OR EXISTS(SELECT 1 FROM "Enrollments" WHERE NOT "IsDemo") THEN RAISE EXCEPTION ''Non-demo data found; reset refused''; END IF; END $$; DELETE FROM "AuditLogs"; DELETE FROM "Enrollments" WHERE "IsDemo"; DELETE FROM "Students" WHERE "IsDemo"; DELETE FROM "Guardians" WHERE "IsDemo"; DELETE FROM "Faculty" WHERE "IsDemo"; COMMIT;'
    if ($LASTEXITCODE -ne 0) { throw 'Demo reset rejected or failed.' }
} finally { Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue }
Write-Host 'Local demo operational data reset. Restart API to regenerate fixed-seed dataset. Reference institutions and login identities preserved.'
