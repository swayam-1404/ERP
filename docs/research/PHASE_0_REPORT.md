# Phase 0 — Discovery and verification

Date: 2026-10-14, Asia/Calcutta. Status: accepted for local implementation by the follow-up instruction on 2026-10-14. This report records discovery findings; current implemented scope and tests are in ../PROGRESS.md and ../TEST_RESULTS.md.

## Outcome

The official VHEI portal was accessed and rendered. Eight higher institutions are recorded with source provenance; VIT and VDC have additional institutional evidence. VHSS has an official presence but its inclusion in VHEI administration remains unresolved. See [inventory](institution_inventory.md), [data](verified_institution_data.json), [source register](source_register.csv) and [website analysis](website_analysis.md).

## Coverage

| Requested research category | Finding / evidence | Missing before production |
|---|---|---|
| Names and hierarchy | S001 eight named higher institutions; S030 VHSS candidate | Legal operators and shared governance |
| Campus/address/contact | S001, S004, S014, S033 public campus/office references | Postcode conflict; institution-campus mappings |
| Logo/branding | S001/S004 display logos and copyright notices | Asset permission and theme approval |
| Departments/programmes/branches | S005 VIT; S011 VDC navigation; S001 others | Current sanctioned intake, curriculum and codes |
| Affiliations/approvals/accreditation | S001/S004 institutional claims; S007 disclosure available | Regulator verification and effective-year approval letters |
| Administration and faculty | S004, S005, S013 public designations/directory | Current assignment authority; never auto-create accounts |
| Admission/eligibility | S002 enquiry; S012 SAMS workflow | Current prospectus, cutoffs, reservation and exceptions |
| Calendar/exam instructions | S006 references; S023 historical metadata | Current PDF contents and academic rules |
| Fees/scholarships | No fee figures verified; S018 image-based page | Approved rates, eligibility, concessions/refunds |
| Library/hostel/transport | S004 facilities; S011 transport description; S019 image body | Lending/allocation/subscription policies and capacity |
| Training/placement | S020 published activities | Approved recruiters, actual eligibility and drives |
| Notices | S023 historical notices; S001 current admissions announcement | Current institutional notice records |
| Public policies/forms/downloads | S017 image rules; S006 forms; S009 PDF available | Visual extraction, effective dates and policy review |
| Quality assurance | S021/S022 extracted tables lack document rows | Actual evidence documents and verified accreditation |

Only official published reference facts are eligible for future reference seeds. Academic rules, fee amounts and certificate authority remain empty; synthetic records will be isolated and labelled DEMO. Source status distinguishes publication provenance from legal/operational authority.

## Design deliverables

- [Requirements and feature traceability](../architecture/requirements_matrix.csv): every feature in the supplied module list, plus platform/security/data/deployment requirements, has an ID, owner, phase and validation gate. All implementation statuses are NOT_STARTED.
- [Requirements specification](../architecture/requirements.md): workflows, role boundaries and definition of done.
- [Initial PostgreSQL entity relationships](../database/entity_relationships.md): proposed keys, institution isolation, accounting and certificate lifecycle. No migration has run.
- [Roadmap and first milestone](../architecture/roadmap.md): Phase 1 deliverables and execution gates.

## Validation and limits

Public source reads and HTTP retries were executed. JSON/CSV consistency and prompt-feature coverage will be checked locally. Backend/frontend/database, browser ERP flows, Docker service health and mobile tests have not run because application code has not been created. Tool availability was checked; dotnet was not found on PATH. See ../TEST_RESULTS.md.

## Decisions requiring review

Accept Phase 0 to start Phase 1, as explicitly required by the supplied prompt. Confirm VHSS scope, postcode and VIT branch discrepancies before those records are activated. Institutional fees/rules/signature credentials and production access are later dependencies; local foundations can proceed after Phase 0 acceptance without inventing them. No server deployment, DNS change, real payment or live institutional CMS mutation is authorized by this report.
