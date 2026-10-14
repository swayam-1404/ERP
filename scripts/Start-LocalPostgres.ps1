$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Import-LocalEnvironment.ps1')
$pgBin = Join-Path $projectRoot '.tools\pg-runtime\pgsql\bin'
$pgData = Join-Path $projectRoot '.local\postgres-data'
$pgLog = Join-Path $projectRoot '.local\postgres.log'
if (-not (Test-Path -LiteralPath (Join-Path $pgBin 'initdb.exe'))) { throw 'Project-local PostgreSQL binaries missing. Use Docker Compose or install official PostgreSQL binaries.' }
New-Item -ItemType Directory -Force -Path (Join-Path $projectRoot '.local') | Out-Null
if (-not (Test-Path -LiteralPath (Join-Path $pgData 'PG_VERSION'))) {
    $pgPasswordFile = Join-Path $projectRoot '.local\pg-init-password'
    [IO.File]::WriteAllText($pgPasswordFile, $env:POSTGRES_PASSWORD)
    try { & (Join-Path $pgBin 'initdb.exe') -D $pgData -U vhei --pwfile=$pgPasswordFile --auth=scram-sha-256 --encoding=UTF8 --locale=C; if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL initialization failed.' } }
    finally { Remove-Item -LiteralPath $pgPasswordFile -ErrorAction SilentlyContinue }
    Add-Content -LiteralPath (Join-Path $pgData 'postgresql.conf') -Value "`nlisten_addresses = '127.0.0.1'`nport = 54329`nmax_connections = 50`nshared_buffers = '128MB'"
}
& (Join-Path $pgBin 'pg_ctl.exe') -D $pgData status *> $null
if ($LASTEXITCODE -ne 0) { & (Join-Path $pgBin 'pg_ctl.exe') -D $pgData -l $pgLog -w start; if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL startup failed. Check .local/postgres.log.' } }
$env:PGPASSWORD = $env:POSTGRES_PASSWORD
try {
    $exists = & (Join-Path $pgBin 'psql.exe') -h 127.0.0.1 -p 54329 -U vhei -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='vhei'"
    if ($exists -ne '1') { & (Join-Path $pgBin 'createdb.exe') -h 127.0.0.1 -p 54329 -U vhei vhei; if ($LASTEXITCODE -ne 0) { throw 'Database creation failed.' } }
} finally { Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue }
Write-Host 'Local PostgreSQL ready on 127.0.0.1:54329.'
