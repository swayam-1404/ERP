# Executed test results — 2026-10-17

## Admissions increment

Backend: 39 real-PostgreSQL integration/security tests passed, 0 failures/skips (23 seconds in the final run), including eight new admissions tests. The original student/faculty/attendance/security checks passed alongside the new cases. Raw result: backend/tests/VHEI.IntegrationTests/TestResults/admissions-validation.trx (ignored). Frontend production build passed after the final accessibility/layout changes. Discovery consistency: 16 passed, 0 failed. The AdmissionIntake migration applied successfully to disposable integration databases and the local demo database.

Final full Playwright run: 9 passed, 0 failed (reported 1.2 minutes), including both admissions checks and all seven existing core/teaching checks. Runtime API readiness returned ready and the web app remained available on localhost. GitHub CI is not inferred from these local results.

Browser checks add application registration/edit/submission, independent evidence review/cancellation, persisted history after reload and a 390px mobile filter/empty-state page-width check. Desktop, history and mobile admissions screenshots were inspected under testing/screenshots/admissions-*.png and admission-history.png. See testing/ADMISSIONS_INCREMENT.md and api/ADMISSIONS_API.md for scope and limitations.

Earlier failures: the concurrent duplicate test compared an original mixed-case generated number against the intended uppercase stored value; corrected the expectation, then the full backend suite passed. The first browser run passed eight checks and failed the mobile status selector's exact accessible name; added an explicit aria-label and both admissions browser tests passed in a focused rerun. The full suite was rerun after consolidating login helpers to wait only on actual 429 responses from the unchanged production-default limiter, so growth of the test suite does not require weakening authentication controls. Authentication errors other than rate limiting still fail immediately.

No new runtime dependencies, official policies, real institutional records, payment processing or production deployments were introduced. Earlier dependency scan results below are dated historical checks, not a fresh vulnerability scan.

## Earlier teaching/core checks — 2026-10-14

| Check | Actual result |
|---|---|
| ASP.NET Core 10 solution build | PASS; 0 warnings, 0 errors |
| Backend integration/security suite | PASS; 31 tests, 0 failed, 0 skipped; actual isolated PostgreSQL |
| React TypeScript/Vite production build | PASS; route bundles split below initial large-chunk warning |
| npm dependency audit | 0 known vulnerabilities reported after pinned lock regeneration |
| Playwright browser suite | PASS; 7 tests, 0 failed |
| Live database/API health and proxy | PASS; actual PostgreSQL + same-origin API response |
| Desktop/mobile screenshot inspection | Completed; chart rendering fixed; mobile page-width check passed |
| Docker development Compose parsing | PASS; config --quiet returned without error |
| Discovery validator | Earlier 16 checks passed; updated validator run recorded in testing/discovery-validation.json |

Backend scenarios: unavailable database 503/no secret leak, independent liveness, anonymous rejection, foreign institution read/write rejection, restricted faculty write/private student/export denial, CSRF enforcement, audited writes, HttpOnly cookies/logout, deterministic 500-row seed/idempotency, atomic student/guardian/enrollment creation, invalid request/no orphan, foreign student ID/programme denial, duplicates, simultaneous edits (one success/one conflict), history-preserving archive, filtered/scoped export, privileged Production MFA setup/login enforcement, production DEMO seed rejection and login rate limiting.

Browser scenarios: login/institution switching/academic source/departments/audit/logout; student create/search/view/edit/CSV/archive; separate faculty navigation; desktop chart + 390px mobile directory/no page overflow; security settings and faculty profile creation. Screenshots: testing/screenshots/dashboard-desktop.png and students-mobile.png. Backend raw result: backend/tests/VHEI.IntegrationTests/TestResults/core-validation.trx (local ignored test artifact).

## Earlier failures fixed

Initial OpenTelemetry 1.15 restore rejected a known advisory; versions updated to current 1.19 packages. Initial reference-content relative path was corrected. PostgreSQL full-archive extraction was slow; selected official runtime directories extracted and used. Browser first launch hit EBUSY during install; completed Chromium used. Windows API binary locks resolved by stopping only this project's API. Browser logout stale cache fixed by refreshing the signed-out page. Expanded backend fixture exhausted normal login rate limit; fixture budget increased with an independent limiter enforcement test (runtime default remains 10/minute). Test archive locator matched an option and status badge; narrowed to table. Type checker caught Zod API mismatch; schema uses installed-version-compatible string validators. Chart screenshot was taken during animation; deterministic rendering and SVG sector check added.

## Not executed / not complete

Docker image/service startup; GitHub Actions remote run; Hostinger specs/staging/deployment; full clean-machine bootstrap and demo reset rehearsal; finance/marks/documents/certificate/PDF tests; attendance concurrency; Flutter device/emulator; broad accessibility audit; load/capacity measurement; encrypted backup/restore/rollback. No performance or production-readiness claims made.

Final package vulnerability scan: dotnet list package --vulnerable --include-transitive reported no vulnerable packages in all five projects against the current NuGet sources. Final discovery consistency validator: 16 passed, 0 failed. Pinned npm lock reproduced by npm ci --dry-run --ignore-scripts. These checks are time-specific dependency results, not a full security audit.

## Teaching/faculty increment validation

Final backend build: 0 warnings/errors. Real PostgreSQL suite: 31 passed, 0 failed, 0 skipped (44 seconds in the final recorded run). Final Playwright suite: 7 passed, 0 failed (reported 1.2 minutes). Updated frontend production build passed. Discovery consistency: 16 passed, 0 failed. Current raw backend result: backend/tests/VHEI.IntegrationTests/TestResults/attendance-validation.trx. TimetableAttendance migration applied to local development database and verified in disposable test databases.

New coverage: faculty edit/archive/version and scope; room/faculty/cohort exclusions with simultaneous bookings and adjacent slots; class roster and capacity; full attendance submission and repeat rejection; assigned/foreign class boundaries; cancellation history; independent correction review and replay denial; operational percentage; fixed demo account association despite additional earlier profiles. Browser checks now include actual class scheduling, faculty submission, correction request, administrator approval/report, room configuration and faculty edit/archive. Attendance screenshots inspected.

Earlier failed runs: PostgreSQL not ready at fixture start; background API content root missed logging configuration; cold startup caused browser timing failures; accessible faculty selector name was missing; demo portal association selected an extra profile; multi-account test raced logout navigation. Corrected launcher/readiness, selector labels, deterministic audited mapping (with regression test), and browser navigation waits; final suites passed. No attendance policy, financial, degree, production capacity or deployment claims are inferred.
