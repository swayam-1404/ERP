# Admissions increment — 2026-10-17

Scope: persistent application drafts, institution/programme/session selection, external counselling reference, filtered tracking, status totals, independent evidence review/rejection, reasoned cancellation and retained history. React route `/admissions`, scoped API and AdmissionIntake migration are integrated with the existing workspace.

Verification coverage added: atomic independent review without enrollment, required evidence, duplicate normalized concurrent creates, simultaneous draft edit/submission, immutable submitted details, rejected/cancelled replay denial, foreign references/resources/institution access, faculty privacy, invalid fields, CSRF and scoped search/counts. Browser coverage registers/edits/submits as institution administrator, reviews/cancels as another administrator, reloads persisted results and checks mobile filters/empty-state width.

Limits: manually recorded references only; uploads, automatic eligibility policy, enquiry pipeline, applicant portal, admission approval, finance/payment, enrollment conversion and full admissions reporting remain pending. No production deployment performed. See TEST_RESULTS.md for executed counts and outcomes.

Final execution: 39 backend tests passed, 9 browser tests passed, frontend production build passed and 16 discovery checks passed. Earlier duplicate-normalization test expectation and exact selector name failures were fixed and final suites rerun successfully.
