# Portal, examination and document validation — 2026-10-20

Added migrations StudentGuardianPortal, ExaminationsResults and AcademicCertificates. Tests use real disposable PostgreSQL databases and institution-isolation boundaries.

Portal coverage: own and guardian relationships, multiple wards, no directory/teaching/finance administration, email/role-escalation rejection, first password change, CSRF, archive and immediate revoked-cookie denial. Browser covers verified grant, temporary-password change, portal use and revocation.

Examinations: source-backed independent policy approval, exact saved roster, component limits/absence semantics, no early submission, full workflow/independent result approval, publication privacy, immutable published marks, faculty assignment, foreign scope and concurrent versions. Calculation tests independently check weighted GPA, component failure and rounding/pass boundaries.

Documents: student transcript request, independent issuance, duplicate pending requests, concurrent single issuance, byte-for-byte repeated download, stored SHA-256, minimal public verification, revoked status/download denial, scope/CSRF, changed review snapshots, cancellation/replacement, explicit curriculum approval, missing subjects/minimum credits/manual clearance gates, unsupported-script rejection and multi-page layout stress. A future dedicated production-mode certificate test and authorized signing/institutional template acceptance are still required before production issuance.

PDF QA: generated an intentionally long 18-subject transcript with a 120-character name/subject and 500-character authority reference, rendered all four pages with Poppler, inspected every page in a contact sheet and extracted all 18 subject labels. QR is drawn as vector modules; the original PNG route failed PDFsharp decoding and was replaced. No clipping or missing rows observed. QA PDF is local ignored test data, not an institutional template approval.

The first expanded suite passed all 78 test bodies but exited unsuccessfully when a concurrent disposable-database checkpoint/drop exceeded the default command timeout. Test collections now use at most two parallel database fixtures with a 120-second command allowance; explicit simultaneous-business-operation tests retain their concurrency. Final run results are recorded in TEST_RESULTS.md. No failed cleanup run is represented as a clean pass.

Production deployment, full official curricula, digital signatures, load/restore/accessibility/device validation, supplementary modules and broader finance remain outside this increment.
