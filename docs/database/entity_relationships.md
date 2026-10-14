# Initial PostgreSQL entity relationship proposal

Design only. No database, schema or migration exists yet. UUID primary keys, UTC timestamptz, explicit institution scope, numeric money/credits, optimistic version columns.

## Entity inventory by bounded module

| Domain | Proposed entities |
|---|---|
| Core | Institutions, Campuses, InstitutionCampuses, Departments, Programmes, ProgrammeVersions, AcademicSessions, Semesters, Courses, Subjects, Sections, SourceEvidence |
| Identity | Users/Identity tables, Roles, Permissions, RolePermissions, UserInstitutionAssignments, UserDepartmentAssignments, UserStudentLinks, GuardianStudentLinks, Sessions |
| Admissions/SIS | Enquiries, Applications/Admissions, ApplicationDocuments, VerificationDecisions, CounsellingReferences, Students, Guardians, Enrollments, SubjectRegistrations, Transfers, Scholarships, StudentScholarships, Documents |
| Faculty/HR | Faculty, Staff, TeachingAssignments, Workloads, LeaveRequests, LeaveApprovals, StaffAttendance, PayrollRuns, PayrollItems, SalarySlips, RecruitmentCases, Appraisals, ServiceRecords |
| Timetable/attendance | Rooms, Laboratories, Timetables, TimetableSlots, ClassSessions, AttendanceRecords, AttendanceCorrections |
| Exams | Examinations, ExamRegistrations, AssessmentComponents, Marks, MarkRevisions, GradingRuleVersions, GradeBands, Grades, Results, ResultItems, ResultApprovals, Backlogs |
| Finance | FeeStructureVersions, FeeItems, StudentInvoices, InvoiceLines, Payments, PaymentAllocations, Refunds, Accounts, JournalEntries, LedgerEntries, Reconciliations, IdempotencyRecords |
| Certificates | GraduationEligibility, CertificateTemplates, CertificateTemplateVersions, CertificateRequests, CertificateApprovals, IssuedCertificates, CertificateVerifications, CertificateReissues, ExternalDegreeRecords |
| LMS | Materials, Assignments, Submissions, Quizzes, QuestionBanks, Questions, QuizAttempts, SyllabusProgress |
| Library | LibraryBooks, BookCopies, BookTransactions, Reservations, Renewals, LibraryFines |
| Hostel/transport | Hostels, HostelRooms, HostelAllocations, GatePasses, Complaints, TransportRoutes, RouteStops, Vehicles, Drivers, Subscriptions, MaintenanceRecords |
| Placement | StudentPlacementProfiles, Companies, Recruiters, PlacementDrives, DriveEligibilityRules, DriveRegistrations, AptitudeTests, Interviews, Internships, Offers |
| Compliance | EvidenceRecords, AffiliationDocuments, Committees, CommitteeMembers, Meetings, Grievances, AntiRaggingCases, ComplianceReminders |
| Communication | Notices, News, Events, PublicPages, GalleryAssetReferences, Achievements, Notifications, DeliveryAttempts |
| Inventory/facilities | Assets, Equipment, Vendors, PurchaseRequests, PurchaseOrders, PurchaseOrderLines, StockMovements, MaintenanceRequests, AssetAssignments, FacilityBookings |
| Research/alumni | Publications, Projects, Grants, Seminars, Workshops, MOUs, AlumniRecords, AlumniEvents, AlumniRequests |
| Shared workflows | ApprovalWorkflows, ApprovalSteps, WorkflowInstances, AuditLogs, OutboxMessages, JobExecutionRecords |

## Key relationships

- Institution 1:N Departments, Programmes, Sessions and operational records; Institution N:M Campus through InstitutionCampuses. Programme N:1 Department, Programme 1:N effective ProgrammeVersions. Version 1:N Semesters/Courses/Subjects and grading-rule assignments.
- Student 1:N Enrollments; Enrollment N:1 ProgrammeVersion/Session/Section, 1:N SubjectRegistrations. Student N:M Guardians via GuardianStudentLinks. Faculty N:M subjects/sections via TeachingAssignments.
- TimetableSlot → section, subject, faculty, room; ClassSession → timetable slot; AttendanceRecord → class session + enrollment/registration. Unique student/session record; corrections reference original record and approval.
- Examination → programme version/session; AssessmentComponent → subject/exam; Marks → student registration/component; MarkRevision preserves old/new scores, actor, reason and approval. Result → enrollment/exam/rule version; ResultItems retain exact approved marks/credits/grade snapshot. Backlogs reference failed result items and clearance attempts.
- Invoice → enrollment; InvoiceLine → fee version/item. Payment N:M Invoice through PaymentAllocations; Refund → original payment/approval; JournalEntry 1:N LedgerEntries with account/debit/credit. Approved scholarship/concession is independently referenced.
- CertificateRequest → student/enrollment/type/template version; CertificateApprovals record sequential exam and officer decisions; IssuedCertificate → approved request + result snapshot + hash + private object reference. Reissue references prior issuance; revoked documents remain in registry. University degrees are ExternalDegreeRecords.
- SourceEvidence 1:N reference facts; reference values preserve source URL/title/date/status/effective year. Notifications and outbox messages reference authorized domain events, never arbitrary external delivery instructions.

## Institution integrity

Every scoped table carries institution_id. Add UNIQUE(institution_id,id) on parent rows; child FKs use (institution_id,parent_id) so a tampered write cannot create cross-institution links. Global Identity user IDs are linked through assignments, not duplicated as tenant users. EF query filters are a convenience, not sole isolation. Policies check actual assignments/department/student relationship. PostgreSQL RLS is a planned defense with SET LOCAL institution context inside transactions, restricted app role and audited central reporting path. Verify connection-pool resets and deny absent context. Never grant application BYPASSRLS.

## Constraints and transactions

Unique institution/student number, enrollment/session/programme, attendance/student/session, mark/component/registration, certificate registry number and institution/idempotency key. Timetable and room allocations require interval-overlap checks with transaction-safe PostgreSQL exclusions where applicable; hostel occupancy locked/rechecked during allocation. Money >=0 for individual payment amounts; ledger lines exactly one debit/credit and posted journal totals equal enforced transactionally, with deferred checks considered in migration design. Idempotency records bind request hash and response to authenticated scope.

Enrollment, attendance batch submission, results approval/publication, posting/refunds and issuance run in explicit transactions. Outbox guarantees delivery after commit; jobs are retry-safe. Never soft-delete posted journals, mark revisions, issued certificates or audit history. Retention/anonymization follows approved policy. Private objects use opaque keys, short-lived authorized downloads, encryption and integrity hash.

## Index/migration/seed strategy

Index (institution_id,status,created_at), student/enrollment relations, session/subject attendance, exam/result lookup, due invoices, certificate token hash/serial and audit date. Avoid unbounded global scans. EF migrations own schema; SQL reviewed for FKs/RLS/checks. CI starts real PostgreSQL, applies migrations and runs integration/isolation tests. Seed official facts only after provenance validation; demo seed separate and guarded by Development environment, fixed seed 20261014, reserved example.test/example.com addresses and no routable phone numbers. Production seed contains minimum roles/configuration, no students, finance, marks or certificates.
