# Admission finance increment — 2026-10-17

Connected lifecycle: independent fee-plan approval → reviewed application charge → pending payment evidence → independent confirmation → final admission authority approval → atomic student/guardian/enrollment conversion. New Finance workspace includes plan approvals and scoped charge/dues records; application details include the financial steps and acknowledgements. Admission statuses now include Approved and Enrolled.

Backend coverage added: precise positive amounts, independent/year-matched fee plan approval, no charge from unapproved/foreign programme plans, confirmation/final approval gates, payment idempotency and concurrent overpayment protection, external reference uniqueness, rejected payment balance release/replay denial, paid cancellation protection, duplicate student rollback, simultaneous conversion exactly once, acknowledgement gating/demo label, scoped privacy/CSRF, unpaid cancellation/void and VDC +3 external portal/selection requirements.

Browser coverage adds fee-plan entry as institution administrator, approval/payment verification/final approval by another administrator, conversion, confirmed dues/acknowledgement visibility, and mobile finance width/empty states. Existing admission/teaching/core checks remain in the regression suite.

Official amounts are not seeded or automatically charged. Current website research is documented separately with source years and conflicts. This is an admission subledger with manually verified payment records; it is not the complete finance module, general ledger, gateway or SAMS integration. Production deployment, true bank reconciliation, refunds and institutional acceptance remain pending. See TEST_RESULTS.md for actual execution outcomes.

Final execution: 55 backend tests and all 13 browser tests passed; backend/frontend builds passed and 16 discovery checks passed. Desktop/mobile screenshots inspected; sidebar scrolling fixed after adding the Finance navigation item. No final failures/skips.
