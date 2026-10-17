# Progress — 2026-10-17

## Current state

Working local institutional/student/teaching core with admissions intake and independent evidence review. Full ERP remains in progress. Source conflicts have operational resolution rules, preserving evidence. Feature matrix uses PARTIAL for implemented increments whose broader requirements remain incomplete.

| Phase | State | Validated delivery / remaining scope |
|---|---|---|
| 0 | Accepted for local implementation | Research, 34 sources, 99 fact references, 219 requirements; unresolved official policies retained |
| 1 | Local foundation validated; container check pending | .NET/React build, real PostgreSQL, migrations, live health/UI; Docker daemon unavailable |
| 2 | Core increment validated; broader phase partial | Identity, MFA, policy scope, references, department create/audit; campus/semester/roles/approval/notices remain |
| 3 | Student/faculty and admissions increments; broader phase partial | Student/guardian/enrollment maintenance, faculty maintenance, 500 DEMO students; application drafts/tracking/external references/independent evidence review/rejection/cancellation/history. Admission approval, fees, conversion, secure documents and broader lifecycle remain |
| 4 | Teaching/attendance and weekly-series increments validated; broader phase partial | Dated and weekly schedule, explicit skip dates, conflict previews/atomic saves, future cancellation, date filters, saved rosters, attendance, separate correction review and reports; practical/template editing/shortage/student-guardian features remain |
| 5–12 | Not started | Exams/results, finance, certificates, additional modules, Flutter, integrated/load/restore tests, deployment and handover |

## Current increment report

Completed and integrated features: listed in README and API guides. Files changed: backend domain/context/reference and demo seed/6 EF migrations/API/security/tests; frontend app/services/components/styles/student/faculty/admissions/attendance/account-security/dashboard/browser tests; infrastructure Docker/Nginx/Compose, .github CI, environment/run scripts and documentation. See docs/testing/PHASE_1.md and PHASE_2_CORE.md for earlier validated checkpoints.

Database changes: InitialCore, InstitutionCore, StudentFacultyCore, TimetableAttendance, AdmissionIntake and RecurringTimetable migrations; institution/Identity, department/programme/session/assignment/audit, student/guardian/enrollment/faculty plus admissions/teaching/attendance/correction/weekly-series tables. Composite institution FKs and optimistic version checks. Seeds: source-backed references, explicit demo sessions and fictional accounts; deterministic seed 20261014 produces 500 students/guardians/enrollments and 32 faculty. Browser verification adds labelled demo records.

Tests executed: backend build (0 warnings/errors), 46 real-PostgreSQL integration/security checks, React build, 11 Playwright browser checks; desktop/mobile screenshots inspected; Compose development config parsed. Final test failures: none. Earlier failures and fixes are recorded in TEST_RESULTS.md. CI workflow authored; remote result not verified.

Known issues: broader phases incomplete, official rules/fees/authority unconfigured, Docker service startup untested, native bootstrap/reset not rehearsed on a clean second machine, full accessibility/performance/backups not measured.

Remaining work and next step: complete remaining core/admissions, practical scheduling/template editing, official shortage rule configuration and student/guardian access before claiming full phase completion. Never mark mandatory certificate or finance features complete merely because their design exists.

Git: feature/erp-core, repository swayam-1404/ERP only. Teaching/admissions and recurring timetable increments use draft PR #1. No merge or deployment performed.

## Admissions intake increment — 2026-10-17

Adds AdmissionApplications/AdmissionEvents through AdmissionIntake migration, scoped Manage APIs and the `/admissions` React route. Drafts can be edited, then submitted for another administrator's evidence review/rejection. Referenced policy, eligibility evidence and document verification are recorded manually. Cancelled/rejected applications retain history. Writes use optimistic concurrency and atomic event/audit persistence. Summary counts refer to applications, not confirmed admissions. No automatic student enrollment or fee confirmation. API details: api/ADMISSIONS_API.md. Executed validation: TEST_RESULTS.md.

## Recurring timetable increment — 2026-10-17

Adds TimetableSeries and optional scoped class-series links. Weekly patterns materialize 1–64 dated periods across up to 112 days in explicit IST, with manual skipped dates, advisory conflict preview and atomic saves protected by existing database exclusions. Future-remainder cancellation locks/version-checks the series and classes while preserving past attendance and rosters. Class schedule date filters expose periods beyond the default range. Template editing, practical groups, automatic holiday calendars and official shortage/eligibility policies remain unfinished. See testing/RECURRING_TIMETABLE.md.
