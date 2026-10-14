# Local setup and run guide

Location: E:\ERP\VHEI-ERP. The working development app is served at http://127.0.0.1:5173; API at http://127.0.0.1:5100. PostgreSQL listens on 127.0.0.1:54329. Private development credentials are in ../../.local/access.md (excluded from Git).

## On this computer

From the project folder in PowerShell:

```powershell
./scripts/Start-Development.ps1
```

Use admin@example.test for the central view, institution@example.test for VIT administration, or faculty@example.test for the faculty workspace. The generated password is in .local/access.md. Profiles and operations persist in PostgreSQL across restarts; DEMO seeds are idempotent. Browser verification creates additional fictional records and audit events; those records are not official student data.

To build or run tests after changes:

```powershell
./scripts/Stop-Development.ps1
. ./scripts/Import-LocalEnvironment.ps1
./scripts/Start-LocalPostgres.ps1
dotnet build backend/VHEI.slnx
$env:TEST_DATABASE = $env:ConnectionStrings__Erp
dotnet test backend/VHEI.slnx --no-build
npm run build --prefix frontend
./scripts/Start-Development.ps1
npm run test:e2e --prefix frontend
```

Tests create and clean only randomly named vhei_integration_test_* databases. Do not point TEST_DATABASE at a production server; its credentials need database-creation rights for the disposable test fixture. Integration tests do not use an in-memory substitute. Playwright uses Chromium (`npx playwright install chromium` from frontend if missing). Windows running API binaries must be stopped before rebuilding, otherwise build files are locked.

## New Windows checkout

Install Node.js 24 and Git from official sources. Run ./scripts/Initialize-Development.ps1. It preserves an existing .env, generates local secrets when absent, downloads the pinned .NET SDK and PostgreSQL binaries from official vendor sources, builds backend/frontend and writes the local access guide. Then run Start-Development.ps1. Initializer is supplied for reproducibility; the complete new-checkout workflow has not been independently tested on a second machine.

If Docker is available, `docker compose up -d db` can provide PostgreSQL instead of the local binary service. Do not run both on port 54329. Docker daemon startup did not work in this session, so the native PostgreSQL route was the validated runtime. Do not share .env, .local or .tools.

## Logs and database management

.local/api.log, api-error.log, web.log, web-error.log and postgres.log. GET /health/live and /health/ready give liveness and database connection status. Development OpenAPI: /openapi/v1.json. Migrations reside in backend/src/VHEI.Infrastructure/Migrations; explicit updates use `dotnet ef database update --project backend/src/VHEI.Infrastructure` after importing environment. DEMO startup applies migrations; Production rejects DEMO seeding. Never infer production migration/seed approval from this local setup guide.

Stop tracked background app services and gracefully stop PostgreSQL using Stop-Development.ps1. It preserves all data. Services launched manually in other terminals must be stopped in their own terminals.
