# Core API — implemented increment

Prefix /api/v1. Same-origin cookie session; mutations require X-CSRF-TOKEN obtained from GET /auth/csrf. After login or identity changes, obtain a fresh token. 401 means no session; 403 means policy/scope denial; 400 means validation/CSRF failure; 409 means uniqueness or stale version conflict. Unhandled errors use Problem Details without sensitive connection details.

| Method | Route | Permission / behavior |
|---|---|---|
| GET | /auth/csrf | Anonymous session token initialization |
| POST | /auth/login | Identity login, lockout and rate limit; may return requiresMfa |
| POST | /auth/mfa/login | Complete pending authenticator sign-in |
| POST | /auth/logout | End authenticated session |
| GET | /auth/me | Identity and actual institution assignments |
| GET/POST | /auth/mfa/setup | Authenticated setup state / reset unenrolled key |
| POST | /auth/mfa/enable | Verify TOTP, enable MFA and sign out |
| GET | /institutions | Assigned institutions only |
| GET | /institutions/{id}/reference | Scoped department, programme and session references |
| POST | /institutions/{id}/departments | Administrator; locally configured, unverified reference with audit |
| GET | /institutions/{id}/dashboard | Scoped academic counters |
| GET | /overview | Administrator; consolidated assigned-institution counts |
| GET | /institutions/{id}/audit?page=1 | Administrator; 25-row audited action list |
| GET | /institutions/{id}/students | Administrator; search/status/programmeId/page/pageSize filters |
| POST | /institutions/{id}/students | Administrator; student + guardian + enrollment + audit atomically |
| GET/PUT | /institutions/{id}/students/{studentId} | Administrator; profile / version-controlled update |
| POST | /institutions/{id}/students/{studentId}/archive | Administrator; version and reason required; retains history |
| GET | /institutions/{id}/students/export | Administrator; entire scoped directory CSV, formula injection guarded |
| GET | /institutions/{id}/student-summary | Administrator; live dashboard metrics |
| GET | /institutions/{id}/faculty | Assigned institutional directory; search/page |
| POST | /institutions/{id}/faculty | Administrator; profile + department validation + audit; no login grant |

Production privileged Workspace/Manage policies require completed MFA. Development allows fictional administrator accounts without MFA to exercise local workflows. Identity password hashes are never exposed; auth cookies are HttpOnly/Strict, Secure in production, expire at 30 minutes and renew on activity. Privileged role administration/provisioning, recovery codes and external identity integration are not completed.

Student payload: studentNumber, fullName, email, status (Active/NewlyAdmitted/Graduated/Alumni), programmeId, academicSessionId, batchYear, semester, section, guardianName, guardianEmail, version. Status is administrative and does not establish official graduation eligibility. Cross-institution programme/session/guardian/enrollment relations are constrained with composite FKs in PostgreSQL. Faculty do not currently have assigned-teaching student access; private student endpoints deny Faculty role until that scoped workflow exists.

OpenAPI is generated at /openapi/v1.json only in Development. Finance, attendance, marks, documents and certificates APIs are not implemented and must not be represented as passing security checks.
