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

Faculty profile PUT /faculty/{id} requires version plus full profile fields; POST /faculty/{id}/archive requires version/reason and no pending classes. Profiles/account permissions stay separate. Current demo faculty@example.test is explicitly linked to the fixed faculty-vit-1@example.test synthetic profile, never a public real faculty name. Production account provisioning/linking remains pending.

Class creation takes subjectId, facultyId, classroomId, section, startsAt, endsAt (DateTimeOffset); times store UTC and display IST. Classroom capacity checks saved roster size. Subject takes code, name, programmeId, academicSessionId and semester. Attendance takes version and items [{studentId,status}], exactly matching the saved roster. Status: Present, Absent, Excused. 400: invalid roster/time/configuration; 403: role/institution/teaching/self-review denied; 404: scoped resource absent; 409: overlap, duplicate submission, stale version/review or unsafe cancel/archive.

Submitted marks are not involved. Attendance statistics do not establish official exam/graduation eligibility. No shortage alert is sent without approved rules and a configured delivery channel.
