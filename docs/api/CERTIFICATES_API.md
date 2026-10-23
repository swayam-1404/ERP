# Academic documents and curriculum eligibility

Migration AcademicCertificates adds institution-scoped GraduationPolicies and AcademicCertificates. This is a partial Phase 7 increment: academic transcripts and course completion records, with a fixed AC-1 PDF template. It does not issue university degrees, provisional certificates, cryptographic signatures or final semester marksheets.

## Workflow

Manage-only paths beneath `/api/v1/institutions/{institutionId}/certificates`:

- `GET/POST /graduation-policies`: explicit programme/session, requiredSubjectIds (1–100), minimumCredits and sourceReference. Every subject must belong to that programme/session/institution. Another administrator must approve a draft via `POST /graduation-policies/{id}/approve`, version/reason. Approved policies are immutable; publish a new policy for changes.
- `GET /students/{studentId}/eligibility/{policyId}`: requires approved curriculum, matching enrollment, all required subjects published and passed, minimum passed credits, no published programme/session backlogs and no known unpaid admission charges. Other financial/library/administrative clearances are manual evidence because those full ledgers are not implemented.
- `GET/POST /students/{studentId}`: list/request documents. Transcript requires published results. CourseCompletion also requires an eligible approved graduationPolicyId and clearanceReference covering finance/library/administration. This is an attestation reference, not an automatic clearance from missing modules.
- `POST /{id}/issue`: version, reason and authorityReference. Issuer must differ from requester. Locks request/student, rechecks the immutable review snapshot and completion eligibility, generates the PDF, stores original bytes and SHA-256, assigns a unique random document number and 192-bit random verification token, and audits atomically. Concurrent/repeated issues cannot create another document.
- `GET /{id}/download`: authenticated, assigned-institution issued document only; exact stored bytes and no-store response. Revoked downloads return 404.
- `POST /{id}/cancel`: version/reason, Requested only. Use after a changed profile/results snapshot blocks issuance, then create a new reviewed request.
- `POST /{id}/revoke`: version/reason, Issued only. Retains PDF/hash/history and exposes revoked status publicly.

Student/guardian routes `/portal/students/{studentId}/certificates` allow own linked list, POST transcript request and `/{id}/download`; no policy, issuance or revocation powers. Revoked/archived access cannot download.

## Verification and document limits

`/verify/{token}` is a public screen backed by rate-limited `GET /api/v1/certificates/verify/{token}` (30 requests/minute per remote IP). It returns number/type/institution/status/dates/hash/DEMO only; no student name, marks or internal evidence. Invalid tokens return 404. Browser file comparison computes SHA-256 locally without uploading the PDF. QR encodes the configured public base URL; never derive it from an untrusted Host header. Responses prohibit caching and indexing.

`Certificates:PublicBaseUrl` must be an absolute HTTPS address in Production. `Certificates:IssuanceEnabled` defaults false outside Development; institutional authority, reviewed templates, retention/storage policy and signature arrangements must be configured before enabling it. Current AC-1 is a fixed review template with explicit authority reference and no simulated signature/seal. Development or fictional students always produce DEMO-labelled documents/numbers. Latin-script font support only: unsupported scripts fail with a reviewable error rather than silently losing names. Odia shaping/templates, semester/consolidated marksheets, bulk/background generation, formal duplicate/correction lineage, alumni lifecycle and authorized signatures remain pending.

PDFsharp 6.2.4 and QRCoder 1.8.0 use MIT licensing; embedded Noto Sans carries its SIL OFL in Assets/NotoSans-LICENSE.txt. Explicit System.Security.Cryptography.Pkcs 10.0.13 overrides PDFsharp's vulnerable older transitive minimum. See testing/PORTALS_EXAMS_CERTIFICATES.md for executed validation; dependency audits are time-specific, not a full security certification.
