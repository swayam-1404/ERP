# Admissions intake and evidence review

Prefix: `/api/v1/institutions/{institutionId}/admissions`. All routes require an assigned institution and the Manage policy; faculty cannot read applicants. Mutations require CSRF; privileged production users also require MFA.

| Method / path | Behavior |
|---|---|
| GET / | Search name/application/external reference, optional exact status, page (25 records) |
| GET /summary | Institution-scoped counts by application status |
| GET /{id} | Application and retained chronological action history |
| POST / | Register draft |
| PUT /{id} | Edit current draft only; exact version required |
| POST /{id}/transition | Submit, Review, Reject or Cancel; exact version and action reason required |

Draft body: `applicationNumber`, `fullName`, `email`, `programmeId`, `academicSessionId`, `externalReference` (empty allowed), `version` (default 1). Application numbers are normalized to uppercase and unique within an institution; programme/session composite foreign keys enforce the same institution.

Transition body: `version`, `action`, `reason`; Review additionally requires `policyReference`, `eligibilityNote`, `documentReference`. These are manually recorded references to evidence held outside this system, not uploaded documents, automatically verified records or an ERP-approved policy configuration.

Transitions: Draft → Submitted → Reviewed or Rejected. Draft, Submitted and Reviewed may become Cancelled with a reason. Only Draft details are editable. A review/rejection must be performed by an administrator other than the submitting actor; the database also enforces separate reviewers for review outcomes. Rejected/Cancelled are terminal. Submitted mistakes currently require cancellation and a new application; reopening/versioned resubmission remains pending.

Every successful write atomically persists an AdmissionEvent and an institution audit entry. Audit entries contain version/status, not applicant contact/evidence text. Event notes and evidence are restricted to Manage users. No delete endpoint exists. Optimistic concurrency yields 409 for stale/racing actions and rolls back losing history entries. Unknown/cross-institution resource IDs return 404 within authorized institutions; unauthorized institutions return 403. Duplicate normalized numbers return 409.

Reviewed is an evidence-review checkpoint. Separate admission-finance routes implement approved fee plans, payment evidence verification, final approval and atomic student conversion; see ADMISSION_FINANCE_API.md. The generic `/transition` does not provide an Enroll action. Status filters also accept Approved and Enrolled. Pending/paid records block generic cancellation; unpaid cancellation voids its charge. Official eligibility automation, seat allocation, secure uploads and portal account provisioning remain pending. Summary counts are application statuses, not automatically confirmed admission totals. Existing direct student record maintenance is separate and must not be represented as an approved admissions conversion.
