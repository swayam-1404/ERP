# Decisions

## Admissions increment — 2026-10-17

D018: distinguish evidence review from final admission approval/payment/enrollment. Reviewed applications never automatically create students or confirm fees. D019: lock submitted applicant details and require a different submitting/reviewing administrator, with version concurrency and transactionally retained history. D020: document verification records external evidence references only until secure uploads and approved eligibility policies exist; do not manufacture official rules.

| ID | Decision | Status and rationale |
|---|---|---|
| D001 | Follow discovery acceptance gate before implementation | Required by supplied master prompt; pending user review |
| D002 | Record eight VHEI-published higher institutions | Verified as directory facts (S001), not current legal approvals |
| D003 | Exclude VHSS from automatic VHEI seed | Candidate membership unverified (S030–S034) |
| D004 | Preserve conflicting address/branch values | No silent normalization or invented programmes |
| D005 | Keep official fees/rules/signature authorities empty | No valid effective-year evidence obtained |
| D006 | Modular monolith and requested stack | Proposed; no incompatible stack substitution |
| D007 | Explicit InventoryFacilities and ResearchAlumni modules | Proposed extension to cover all requested modules |
| D008 | Scope authorization plus composite FKs; evaluate RLS | Proposed defense against cross-institution leaks; requires tests |
| D009 | Three seed modes, fixed demo seed 20261014 | Proposed; production demo rejection mandatory |
| D010 | Public staff references never auto-create privileged identities | Staff authority needs institutional provisioning |
| D011 | No local runtime/production completion claims | Phase 0 documents only; actual tests tracked separately |

## Implementation follow-up — 2026-10-14

User instructed “make erp by removing conflicts”, accepting Phase 0 for local implementation. D001 no longer waits for acceptance. D003/D004 applied through explicit source-priority configuration, preserving evidence. .NET 10.0.402 installed locally; PostgreSQL 17.11 official Windows binaries used because Docker daemon unavailable. Core isolation is assignments + policy/scoped queries + composite FKs (RLS deferred). Current tested scope is partial phases 1–3; all later phases unimplemented.


## Teaching increment — 2026-10-14

D012: enforce dated room/faculty/cohort overlap with PostgreSQL interval exclusions; keep RLS separate as future defense. D013: snapshot roster at class creation, require complete attendance, preserve cancellation history. D014: submitted attendance changes only through independent correction review. D015: operational attendance percentage excludes Excused/unsubmitted records; no official shortage/eligibility rule inferred. D016: development faculty account mapping uses a fixed synthetic profile and audits relinking. D017: background API must load its compiled appsettings and startup must wait for actual readiness.
