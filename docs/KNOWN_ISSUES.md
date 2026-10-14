# Known issues / pending scope

- The 16-module production ERP is incomplete. Current validated scope is the institutional/student/faculty development increment described in README.
- Remaining Phase 2: campuses, semester configuration, permission/role assignment administration, notices and approval workflows. MFA setup/sign-in implemented; recovery codes/administrator recovery procedures pending.
- Remaining Phase 3: admissions approval/cancellation/external selection tracking, admission fee confirmation, production faculty-account provisioning and full workload management, transfers, scholarships, secure documents, subject registrations and complete academic history. Faculty private student access currently denied.
- Timetable/attendance dated-session increment implemented. Remaining Phase 4: recurring templates, practical/lab workflow, subject registration, approved shortage alerts, student/guardian visibility and rescheduling. Later phases (exams/finance/certificates/additional modules/mobile/deployment) not implemented. No disconnected fake module pages.
- Official academic/attendance/fee/eligibility/certificate/signature policies unknown. Reference advertisements are not active programme approvals. VIT source priority/postcode separation and VHSS exclusion are documented operational decisions, not asserted institutional corrections.
- Native PostgreSQL 17.11 is the tested local fallback. Docker daemon did not start; production images/services remain unverified. Flutter 3.41.6 was found but no mobile project/device tests run.
- Local startup and existing-data idempotent seeding tested; complete clean-machine initializer and guarded reset not rehearsed. Reset script removes development audit history as well as synthetic operational records and must never target live data.
- RLS is a proposed additional defense, not implemented. Current enforced isolation uses authenticated assignments, policy checks, scoped queries and composite FKs, with executed rejection tests.
- Secure uploads/S3, Hangfire/SignalR workflows, off-server backups, actual restore, signature licensing and performance/accessibility audits remain pending.
- Production web theming/messaging and real role onboarding require a later release pass. Infrastructure files are preparation, not validated deployment.
- Public staff references never create privileged users; all current seeded accounts/faculty are fictional. Brand logos/photos/documents were not republished.
