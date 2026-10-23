# Student and guardian access

All paths are beneath `/api/v1/institutions/{institutionId}` and enforce the assigned institution, active linked relationship and workspace security policy. Portal users never receive administrator directory, grading drafts or other students' marks.

- `GET /portal/students`: active linked students only; guardians may have several explicitly verified wards.
- `GET /portal/students/{id}`: own profile, guardian/enrollment and subject attendance totals.
- `GET /portal/students/{id}/classes`: saved-roster classes, bounded date range and pagination; own attendance only.
- `GET /portal/students/{id}/fees`: linked admission charges and payments, excluding internal verification evidence.
- `GET /portal/students/{id}/payments/{paymentId}/acknowledgement`: own confirmed acknowledgement.
- `GET /portal/students/{id}/results`: published results and recorded GPA, never unapproved marks.
- Certificate requests/downloads: see CERTIFICATES_API.md.

Manage-only `/portal-access`: paged list, `POST /students/{studentId}` with kind Student/Guardian, email, temporaryPassword for a new account, and evidenceReference; `POST /{id}/revoke` with version/reason. Email must match the relevant student/guardian record. Existing accounts must have exactly the requested portal role. No administrator-role reuse, password replacement or automatic identity creation. Evidence is manually reviewed, not an automated identity check.

New accounts must change their temporary password before accessing institution records. `POST /auth/password/change` requires currentPassword and a distinct strong newPassword; it clears the requirement and signs out. Revocation is checked against the database for every portal operation and takes effect even with an existing login cookie. Regrant/reactivation and password recovery remain pending.

DEMO-only student@example.test and guardian@example.test link to one deterministic fictional student. Private passwords remain in `.local/access.md`. Creating an admission or faculty profile does not itself grant a login.
