$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Set-Location $projectRoot
New-Item -ItemType Directory -Force -Path .tools,.local | Out-Null
if (-not (Test-Path -LiteralPath '.env')) {
    $dbPassword = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(24))
    $adminPassword = 'Dev!' + [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(12))
    @("POSTGRES_PASSWORD=$dbPassword","ConnectionStrings__Erp=Host=127.0.0.1;Port=54329;Database=vhei;Username=vhei;Password=$dbPassword",'ASPNETCORE_ENVIRONMENT=Development','ASPNETCORE_URLS=http://127.0.0.1:5100','Seed__Mode=Demo',"Seed__AdminPassword=$adminPassword") | Set-Content -LiteralPath .env -Encoding utf8
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue) -and -not (Test-Path -LiteralPath '.tools\dotnet\dotnet.exe')) {
    Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile '.tools\dotnet-install.ps1'
    & '.tools\dotnet-install.ps1' -Version 10.0.402 -InstallDir '.tools\dotnet' -NoPath
}
if (-not (Test-Path -LiteralPath '.tools\pg-runtime\pgsql\bin\pg_ctl.exe')) {
    Invoke-WebRequest -Uri 'https://sbp.enterprisedb.com/getfile.jsp?fileid=1260616' -OutFile '.tools\postgres.zip'
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead((Join-Path $projectRoot '.tools\postgres.zip'))
    try { foreach ($entry in $archive.Entries) { if ($entry.FullName -match '^pgsql/(bin|lib|share)/' -and $entry.Name) { $target = Join-Path (Join-Path $projectRoot '.tools\pg-runtime') $entry.FullName; [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null; [IO.Compression.ZipFileExtensions]::ExtractToFile($entry,$target,$true) } } } finally { $archive.Dispose() }
}
. scripts\Import-LocalEnvironment.ps1
& scripts\Start-LocalPostgres.ps1
dotnet tool restore
dotnet build backend/VHEI.slnx
if ($LASTEXITCODE -ne 0) { throw 'Backend build failed.' }
Push-Location frontend
try { npm ci; if($LASTEXITCODE -ne 0){throw 'Frontend install failed.'}; npm run build; if($LASTEXITCODE -ne 0){throw 'Frontend build failed.'} } finally { Pop-Location }
@('# Local development access','', 'URL: http://127.0.0.1:5173','', 'Central administrator: admin@example.test','Institution administrator: institution@example.test','Faculty: faculty@example.test','', "Password: $env:Seed__AdminPassword",'', 'Synthetic local Development only. Do not reuse these credentials.') | Set-Content -LiteralPath '.local\access.md' -Encoding utf8
Write-Host 'Initialized. Run scripts/Start-Development.ps1. Credentials saved in .local/access.md.'
