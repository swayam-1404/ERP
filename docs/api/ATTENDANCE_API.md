# Timetable / attendance API

Prefix: /api/v1/institutions/{institutionId}/academic. Every route requires an assigned institution and Teaching policy (administrator or faculty); writes require X-CSRF-TOKEN. Resource selection never grants permission. Administrators must satisfy the existing privileged MFA policy in production.

| Method | Route | Behavior |
|---|---|---|
| GET | /configuration | Subject/room references; faculty sees only own active linked profile |
| POST | /classrooms | Administrator name/capacity configuration and audit |
| POST | /subjects | Administrator programme/session/semester configuration; scoped FKs |
| GET | /classes | Assigned classes, 25-row pagination; from/to bounds ≤120 days; default −30/+30 days |
| POST | /classes | Administrator subject/faculty/room/section/IST period; saves eligible roster atomically |
| POST | /classes/{id}/cancel | Administrator version/reason; denies attendance-bearing classes; preserves history |
| GET | /classes/{id}/attendance | Administrator or actual assigned active faculty only; saved roster and records |
| POST | /classes/{id}/attendance | Full distinct roster, valid statuses, class begun, current version; one submission |
| GET | /corrections | Administrator queue; faculty sees only own requests; latest 100 |
| POST | /corrections | Assigned teacher or administrator; original/new status plus 5–500 character reason |
| POST | /corrections/{id}/review | Different administrator; locks associated class, rechecks current status, audits decision |
| GET | /report | Scoped subject counts; optional studentId; faculty limited to assigned-class records |
| POST | /series/preview | Administrator-only expansion, roster/capacity validation and current conflict preview; no persistence |
| POST | /series | Administrator-only atomic template + all dated periods, saved rosters and audits |
| GET | /series | Administrator institution-scoped templates/counts; 20-row pagination |
| GET | /series/{id} | Administrator template and its saved dated periods |
| POST | /series/{id}/cancel | Administrator version/reason; cancel only remaining future unsubmitted periods |

Faculty profile PUT /faculty/{id} requires version plus full profile fields; POST /faculty/{id}/archive requires version/reason and no pending classes. Profiles/account permissions stay separate. Current demo faculty@example.test is explicitly linked to the fixed faculty-vit-1@example.test synthetic profile, never a public real faculty name. Production account provisioning/linking remains pending.

Class creation takes subjectId, facultyId, classroomId, section, startsAt, endsAt (DateTimeOffset); times store UTC and display IST. Classroom capacity checks saved roster size. Subject takes code, name, programmeId, academicSessionId and semester. Attendance takes version and items [{studentId,status}], exactly matching the saved roster. Status: Present, Absent, Excused. 400: invalid roster/time/configuration; 403: role/institution/teaching/self-review denied; 404: scoped resource absent; 409: overlap, duplicate submission, stale version/review or unsafe cancel/archive.

Submitted marks are not involved. Attendance statistics do not establish official exam/graduation eligibility. No shortage alert is sent without approved rules and a configured delivery channel.

## Weekly series

Body: `name`, `subjectId`, `facultyId`, `classroomId`, `section`, `firstDate`, `lastDate` (inclusive ISO dates), `startTime`, `endTime` (same-day campus times), `daysOfWeek` (distinct integers, Sunday 0–Saturday 6), `excludedDates` (ISO dates; empty array allowed). A series spans at most 112 days, creates 1–64 periods, and uses a period duration from 1 minute to 4 hours. Dates must lie within 2000–2100. Exclusions must be distinct selected weekdays within that range. No official holiday list or term eligibility is inferred.

The template records Asia/Kolkata; the server explicitly converts IST (+05:30) to UTC without relying on the host timezone. Preview returns each dated period, conflict categories and saved-roster size. Preview is advisory: creation always applies PostgreSQL room/faculty/cohort exclusions. One overlap, including a booking made after preview, rolls back the entire template, all periods, rosters and audits. Existing single-period scheduling shares the same resource, cohort and capacity preparation. Every materialized period remains available through `/classes` with `timetableSeriesId` and uses existing assigned-faculty attendance controls.

Series cancellation takes `{version,reason}`. It locks the template and dated classes, rechecks version and cancels only future, unsubmitted, uncancelled periods; it retains past classes, attendance and rosters. No remaining future periods, stale versions or replay yield 409. Individual dated periods may still be cancelled through the existing endpoint; the template is not modified by those individual cancellations. Template changes, automatic holiday synchronization, split practical/lab groups and rescheduling remain pending; there is no background generator or automatic later roster refresh.

The class schedule UI provides inclusive campus From/Through dates up to 120 days so materialized periods outside the default ±30-day view remain accessible. Faculty can use date filters for their own assigned classes, but cannot manage series templates.
