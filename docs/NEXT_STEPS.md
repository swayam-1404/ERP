# Next implementation steps

No new Phase 0 approval is needed; the follow-up instruction authorized local implementation and conflict resolution.

1. Complete remaining core configuration (campuses, semester/programme versions, role permissions and account provisioning), preserving source facts and policy gates.
2. Complete Phase 3 beyond implemented intake/evidence review and the connected fee-plan/payment verification/final approval/atomic enrollment workflow: secure documents, configured official eligibility, seat allocation, production authority delegation and refund/withdrawal handling. Then faculty-account provisioning/full workload, student subjects/transfer/document/scholarship history. Current payment records reference independently checked external/offline evidence; they do not initiate or automatically verify bank/SAMS transactions.
3. Finish Phase 4 practical/lab groups, template editing/rescheduling, approved shortage rules and student/guardian visibility. Dated classes, weekly series with skips/preview/atomic creation/future cancellation, conflicts, attendance and audited corrections are implemented.
4. Phases 5–7: approved versioned academic results, expand finance beyond the admission subledger (semester/hostel/transport fees, concessions, refunds, reconciliation/general ledger), and authorized marksheet/certificate lifecycle. Official policy missing must fail closed. Historical fee sources: research/FEE_SOURCE_REVIEW_2026-10-17.md.
5. Additional modules, Flutter, integrated security/performance/restore/accessibility testing, reviewed staging/deployment and final manuals.

Use docs/architecture/requirements_matrix.csv as the full scope, mark each item only after its real API/UI/schema/tests/audit are complete. Reuse the running core rather than substituting disconnected screens. Local services and credentials are described in docs/deployment/LOCAL_SETUP.md.
