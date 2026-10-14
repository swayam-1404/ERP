# Implementation roadmap

Phase 0 accepted for local implementation on 2026-10-14. This is the full planned roadmap; phases 1–3 now have validated increments. See ../PROGRESS.md for current scope and remaining work.

| Phase | Increment | Required exit evidence |
|---|---|---|
| 0 | Discovery, evidence, traceability, proposed entities | Review report; validate JSON/CSV/feature coverage |
| 1 | .NET solution, React shell, PostgreSQL Compose, logging/OpenAPI/CI | Backend and web build; database readiness; health route; migration; initial tests and real API UI call |
| 2 | Identity, roles, MFA, permissions, institution core and audits | Login/session checks, admin API integration, tampered-scope rejection |
| 3 | Admissions, students/faculty, guardians/enrollment, 500 DEMO records | Persistent CRUD, validated forms, seed/scenario and isolation tests |
| 4 | Timetable, sessions, attendance and corrections | Conflict/concurrency checks, attendance calculation and audited correction tests |
| 5 | Versioned grading, marks, moderation, approved results/backlogs | Independent calculation tests; unauthorized/unapproved publication denied |
| 6 | Invoices, collections/refunds, scholarships, ledger/reconciliation | Idempotency, concurrent posting, 12-month DEMO coverage and balanced ledger |
| 7 | Marksheet/transcripts, eligibility, approvals, QR certificates | PDF visual checks, issuance/download/revocation/isolation; authorized type enforcement |
| 8 | HR/LMS/library/hostel/transport/placement/compliance/CMS/inventory/research/alumni | Each module independently meets definition of done; no disconnected screens |
| 9 | Flutter student/faculty workflows | API integration and Android device/emulator results |
| 10 | Integrated security, E2E, load, accessibility and recovery checks | Measured reports and failures resolved; no invented capacity claims |
| 11 | Reviewed Ubuntu/Hostinger staging and release preparation | Actual VPS specs, restore/rollback rehearsal; explicit approval for live changes |
| 12 | Manuals and operational handover | Documentation matches validated system |

## First implementation milestone — Phase 1 foundations

Objective: reproducible local development with a live health/status API displayed by React, backed by a reachable PostgreSQL service. No fake dashboard figures.

Planned files: backend/VHEI.slnx; backend/src/VHEI.{Domain,Application,Infrastructure,Api}/*.csproj; backend/src/VHEI.Api/Program.cs and configuration; backend/tests/{VHEI.UnitTests,VHEI.IntegrationTests,VHEI.SecurityTests}; frontend/package.json and src/{app,services,pages,styles}; backend/src/VHEI.Modules/<module>; database/migrations and seeds; docker-compose.yml; infrastructure/docker Dockerfiles; infrastructure/nginx configuration; .env.example; .github/workflows/ci.yml; docs/api, testing, deployment; updated progress files. InventoryFacilities and ResearchAlumni extend the supplied module folder list to cover modules 14–15 explicitly.

1. Install/locate official .NET 10 SDK and verify Docker daemon/Compose, Node, PostgreSQL image and Flutter health. Verify supported package versions before pinning.
2. Initialize feature branch only after checking Git status; repository currently absent. Preserve source prompt/research.
3. Initialize modular monolith and frontend with clean dependencies and environment-bound secrets. Compose local PostgreSQL bound to localhost only; production database unpublished.
4. Implement health/live and health/ready with actual database readiness, OpenAPI, structured logs/traces, error contracts and React status UI. Avoid exposing configuration or secrets in health output.
5. Create initial migration/test fixture and CI build/check jobs. Execute .NET tests, React build/typecheck, PostgreSQL connectivity and Docker startup, browser API UI verification.
6. Record measured results and known issues. Foundation complete only when all checks pass; otherwise report partial status and fix failures.

No application scaffold is created before the Phase 0 acceptance gate. Credentials and official academic/finance rules are later scoped dependencies, not reasons to substitute a different stack.
