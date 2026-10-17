# Next implementation steps

No new Phase 0 approval is needed; the follow-up instruction authorized local implementation and conflict resolution.

1. Complete remaining core configuration (campuses, semester/programme versions, role permissions and account provisioning), preserving source facts and policy gates.
2. Complete Phase 3 admissions beyond the implemented draft/tracking/external-reference/independent evidence review/cancellation increment: secure documents, configured eligibility, authorised approval, seat allocation, payment confirmation and enrollment conversion. Then production faculty-account provisioning and full workload, student subjects/transfer/document/scholarship history. Admission payment confirmation waits for finance workflow; do not falsely enroll through an unverified approval step.
3. Finish Phase 4 recurring/practical timetable, approved shortage rules and student/guardian visibility. Dated classes, conflicts, attendance and audited corrections are now implemented and validated.
4. Phases 5–7: approved versioned academic results, transaction-safe finance and authorized marksheet/certificate lifecycle; official policy missing must fail closed.
5. Additional modules, Flutter, integrated security/performance/restore/accessibility testing, reviewed staging/deployment and final manuals.

Use docs/architecture/requirements_matrix.csv as the full scope, mark each item only after its real API/UI/schema/tests/audit are complete. Reuse the running core rather than substituting disconnected screens. Local services and credentials are described in docs/deployment/LOCAL_SETUP.md.
