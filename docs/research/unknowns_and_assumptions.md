# Unknowns and assumptions

## Decisions for discovery review

1. Accept the eight institutions listed by VHEI as the initial ERP reference inventory; keep operational activation separate. Source: https://vheibargarh.in/.
2. Confirm whether VHSS belongs in this ERP. Its official site uses Krishna Vikash identity, unlike the VHEI directory: https://vhssbgh.krishnavikash.com/Home/About.
3. Resolve postcode difference: VHEI publishes 768040 (https://vheibargarh.in/); VIT publishes 768028 (https://vitbargarh.ac.in/). Preserve both until confirmed.
4. Resolve VIT programme discrepancy: central directory mentions AI & ML and Electrical; VIT homepage advertises CSE, EEE, Civil, Mechanical and MBA. Sources: https://vheibargarh.in/ and https://vitbargarh.ac.in/. Do not create AI & ML intake or equate Electrical and EEE automatically.
5. Confirm current programmes, regulator approvals and departmental hierarchy. Public advertisements are reference facts, not current admission authority.

## Required later, without blocking independent local work

- Current grading, attendance, credit and graduation policies by programme/batch; current SAMS/OJEE/other prospectus and eligibility rules.
- Finance fee schedules, discount/refund authority, accounting chart and receipt numbering; no actual fees verified.
- Certificate issuing/signing authority, authorized document types, retention/revocation policy; QuestPDF license eligibility.
- Current staff assignments, account owner, roles, guardian-consent model, student retention rules and privacy policies. Public role names must not become privileged accounts.
- Branding permission; use a configurable temporary theme until permission is supplied.
- S3, email, SMS and push credentials; institution-approved notification processes.
- VPS access and measured specifications, staging domain, DNS/SSL authority and encrypted off-server backup destination. Prompt's KVM 2 limits are planning assumptions only.
- .NET 10 SDK: absent on command path in this session. Docker installed but daemon/Compose untested; Flutter installed but SDK/device health untested.

## Engineering assumptions, not institutional facts

Modular monolith, UUIDs, UTC timestamps with Asia/Calcutta display, INR decimal accounting, PostgreSQL, server-enforced institution assignments, three separate seed modes, reproducible DEMO seed 20261014. Production must reject synthetic seeds. Security and workflow rules fail closed when official authority is absent.

Approval gate comes from the supplied development prompt: “After Phase 0 is accepted, proceed to Phase 1”. No acceptance has been inferred.
