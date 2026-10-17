# VHEI Campus ERP

A running development increment in E:\ERP\VHEI-ERP using **ASP.NET Core 10, PostgreSQL and React/TypeScript**. This is a tested institutional/student/teaching core, not the completed 16-module production ERP.

**Open:** http://127.0.0.1:5173 · **Private local credentials:** [.local/access.md](.local/access.md) · **Run guide:** [Local setup](docs/deployment/LOCAL_SETUP.md).

## Working now

- Secure Identity cookie login/logout, account lockout/rate limits, anti-forgery and authenticator setup/sign-in. Privileged production access requires completed MFA.
- Eight source-backed institutional references, programme/department views, assigned institution selection, local department creation and audit trail.
- Student creation, profile/guardian/enrollment, search/status/programme filters, pagination, version-controlled editing, audited archiving and institution-scoped CSV export.
- Faculty directory, search, profile creation, versioned editing and audited archiving; creating a profile does not grant login permissions.
- Admissions drafts, programme/session selection, external counselling references, tracking/totals, independent evidence review/rejection, cancellation and retained history. Review is separate from admission approval/payment/enrollment; see [API scope](docs/api/ADMISSIONS_API.md).
- Admission finance: independently approved year-matched fee plans, charge issuance, recorded payment evidence with separate verification, payment acknowledgements/dues, final admission approval and one-time atomic student conversion. VDC +3 requires external-portal evidence and a selection reference. No live gateway, bank/SAMS integration, refunds or general ledger. [API scope](docs/api/ADMISSION_FINANCE_API.md).
- Distinct central/institution administrator and faculty navigation. Live PostgreSQL dashboard totals and consolidated network charts.
- 500 deterministic fictional students and 32 fictional faculty, clearly DEMO, using reserved example.test emails. Browser checks add further labelled demo records. No private student records imported.
- Timetable periods with database-enforced room/faculty/cohort conflict detection, frozen class rosters, assigned faculty attendance, separate correction review and operational attendance reports.
- Weekly series with IST date preview, selected weekdays/skipped dates, atomic conflict-safe creation, future-remainder cancellation and class date filtering. Saved attendance and rosters remain intact.
- EF migrations, local start/stop scripts, pinned dependency lock, CI and Docker/Nginx preparation.

## Validation

Backend build passed with no warnings/errors; 55 PostgreSQL integration/security tests and 13 Playwright browser checks passed; frontend production build passed. Coverage includes admissions, single/weekly scheduling, attendance and mobile page-width checks. Desktop/mobile screenshots were inspected. See [test results](docs/TEST_RESULTS.md). Docker service startup and production deployment were not verified.

## Still to build

Remaining core/role administration; official admissions eligibility/seat allocation and secure documents; refunds/withdrawals; production faculty-account provisioning; subject registrations/scholarships; remaining timetable/attendance features; examinations/results; full finance beyond the admission subledger; marksheets/certificates; additional ERP modules; Flutter mobile; performance/restore testing; reviewed Hostinger staging/production deployment. Historical published fees are documented with academic years and conflicts, not automatically activated. Missing modules have no fake success screens.

## Conflict resolutions and evidence

Phase 0 accepted for implementation by the user's 2026-10-14 follow-up. [Conflict resolutions](docs/research/CONFLICT_RESOLUTIONS.md) preserve each institution's published address, prefer VIT's own programme names, and keep VHSS outside initial scope. Original facts/evidence remain intact. [Research report](docs/research/PHASE_0_REPORT.md), [source register](docs/research/source_register.csv), [requirements matrix](docs/architecture/requirements_matrix.csv), [database design](docs/database/entity_relationships.md), [API guide](docs/api/CORE_API.md), [progress](docs/PROGRESS.md).

## Start

From this folder in PowerShell, run ./scripts/Start-Development.ps1. New Windows checkouts can run ./scripts/Initialize-Development.ps1 after installing Node 24 and Git; see the guide for prerequisites and validation limitations. Docker Desktop was unavailable, so official local PostgreSQL binaries provide the tested runtime. The API/database are localhost only.

Seed modes: None, Reference, Demo. Demo is forbidden outside Development. The reset script needs updated teaching/admission dependency cleanup and isolated rehearsal before use; do not run it against this working dataset. No live deployment, DNS, payment processing or institutional website changes performed.
