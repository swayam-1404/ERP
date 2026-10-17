# Admission finance and conversion

All routes below are under `/api/v1/institutions/{institutionId}` and require assigned-institution Manage access, CSRF for writes and privileged MFA outside Development. Financial resources cannot be accessed by faculty. No gateway, bank or SAMS payment is initiated.

| Method / route | Behavior |
|---|---|
| GET /finance/fee-plans | Optional programmeId/sessionId/status, page/pageSize (25 default, max 100) |
| POST /finance/fee-plans | Immutable draft: name, programmeId, academicSessionId, sourceAcademicYear, sourceReference, authorityReference, amount, collectionMode (Offline or ExternalPortal) |
| POST /finance/fee-plans/{id}/approve | Version/reason; another administrator, matching source/session academic year; VDC +3 requires ExternalPortal |
| GET /admissions/{id}/finance | Scoped charge, immutable referenced plan, payment history, confirmed total |
| POST /admissions/{id}/charge | Version/feePlanId; Reviewed application, approved matching programme/session/mode, one charge per application |
| POST /admissions/{id}/payments | Recorded evidence: idempotencyKey, externalReference, evidenceReference, method, paidOn, amount |
| POST /finance/payments/{id}/review | Approve/note; another administrator confirms/rejects pending evidence exactly once |
| GET /finance/payments/{id}/acknowledgement | Confirmed record only; downloadable plain-text acknowledgement, labelled DEMO for synthetic data |
| POST /admissions/{id}/approve | Version/approvalReference/reason; Reviewed, full confirmed charge, actor different from submitter; VDC +3 selection reference required |
| POST /admissions/{id}/enroll | Version/studentNumber/guardianName/guardianEmail/batchYear/semester/section; Approved only; atomic one-time conversion |
| GET /finance/charges | Scoped paginated charge/confirmed/pending/outstanding report; optional application/name search |

Amounts use INR decimal(18,2), positive with no more than two decimal places and request maximum INR 10,000,000. An approved plan cannot be edited. Historical source years cannot be approved against another year. No waiver/discount is invented and no official monetary values are seeded. See research/FEE_SOURCE_REVIEW_2026-10-17.md.

Pending plus confirmed payments reserve the charge balance. Recording payments locks the application, so concurrent attempts cannot overpay. Identical idempotency-key/payload retries return the saved payment; a reused key with different data or a duplicate normalized external reference returns 409. External references are unique within the institution, even for rejected records; rejection releases the balance but never erases history. Payment dates cannot be future dates. Methods: Cash, BankTransfer, Cheque for Offline plans; ExternalPortal only for portal plans. Evidence is a reference, not a secure uploaded document or automatically verified settlement.

Financial operations, admission transitions, final approval and conversion serialize on the same application row. Payment verification requires a separate recorded/reviewing actor; database constraints also enforce separation. Confirmed acknowledgements receive a unique ACK UUID; they are not statutory tax receipts or bank confirmations. Refunds/reversals are not implemented, so cancellation with any pending or confirmed payment is blocked. Unpaid cancellation voids the charge and retains it; current unique-charge rules require a new application if that charge was wrong. Post-approval withdrawal is a future workflow.

Final approval is separate from evidence review and payment verification, and requires an authority/reference. Conversion rechecks confirmation and writes guardian, student, enrollment, application link/event and audit in one transaction. Replays/concurrent conversion return 409. Duplicate student numbers roll back every new record. Application name/email and original programme/session are copied; no privileged or portal login is provisioned. Existing direct student maintenance remains a separate legacy administrative path, not evidence of a paid/approved admission. Seat allocation, eligibility automation, dedicated finance-role delegation, general ledger, recurring/hostel/transport charges, refunds and full financial reporting remain pending.
