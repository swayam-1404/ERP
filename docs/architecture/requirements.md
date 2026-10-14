# ERP requirements specification

Source: ../requirements/MASTER_DEVELOPMENT_PROMPT.txt. This document is proposed design, not implemented behavior. Full feature IDs and validation gates: requirements_matrix.csv. 16 modules; 190 module features; 29 platform requirements.

## Lifecycle and boundaries

Admission enquiry → externally selected application where applicable → eligibility/document review → approval/payment confirmation → student enrollment → academic/attendance → approved results → finance clearance and graduation eligibility → authorized institutional certificates/external degree records → alumni access. Keep identity, institution scope, academic year and approval history through every step. Cancelled applications must preserve audit/financial references. Guardians access explicitly linked dependents only.

The 16 module boundaries mirror the supplied prompt. InventoryFacilities and ResearchAlumni are explicit additional module folders; Communication/CMS remain an Administration submodule. Each module owns its tables, commands, validation and tests. No generic CRUD screen substitutes for a required workflow.

## Roles and scope

| Role | Proposed authority | Explicit exclusions |
|---|---|---|
| Central management | Assigned consolidated reports and institution configuration | No implicit all-access marks/finance write permission |
| Institution administrator | Assigned institution master data/accounts | Other institutions and privileged-role escalation |
| Principal/HOD | Assigned institutional/department approval queues | Self-approval where maker/checker separation applies |
| Faculty | Assigned teaching sessions, attendance and draft assessments | Unassigned subjects/students and published result changes |
| Student/alumnus | Own published data and authorized downloads | Draft results, other students and unissued documents |
| Guardian | Linked dependent attendance/published information | Unlinked dependents and unrestricted document access |
| Exam cell | Moderation, verification, approved publication workflows | Unversioned grading and unauthorized issuance |
| Finance | Invoices, collections, reconciliation and approved reversals | Silent posted-transaction mutation |
| HR/library/hostel/transport/placement | Assigned operational functions | Unrelated records and cross-institution access |

## Data and interface contracts

API prefix /api/v1, OpenAPI schema, RFC-style problem responses with trace IDs, bounded cursor/page pagination, allowlisted filtering/sorting. Server resolves institution scope from authenticated assignments; client institution ID is only a selector. Validate resource ownership on reads, writes, joins, exports, jobs and SignalR groups. Audits capture actor, institution, correlation, before/after change where suitable and reason, excluding passwords/tokens/private document bodies.

Web uses same-origin HttpOnly secure sessions plus antiforgery on mutations. Mobile authentication/session design requires a documented separate secure flow; reuse authorization policies, never browser cookie assumptions. Use idempotency keys and optimistic concurrency on sensitive operations. Monetary amounts are decimals in INR; posted journals immutable with authorized reversing entries.

Official academic policies are programme/batch-effective immutable versions. Missing rules block official results/certificates. DEMO fees, rules, people and finance stay in development datasets. Background jobs carry institution and actor authority and recheck approval at execution.

## Definition of done

Each matrix item requires actual schema/migration, authorized functional API, connected web form/page, validation, persistence, errors, auditable transitions, unit and PostgreSQL integration tests, institution-isolation tests and updated manuals. Specific workflows additionally need calculation, reconciliation, concurrency and PDF/browser checks. Status remains NOT_STARTED until implementation and executed evidence justify an update.
