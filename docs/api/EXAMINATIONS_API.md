# Examinations and published results

Institution-scoped `/examinations` requires Teaching; configuration, policy approval and result publication require Manage. Faculty can read/enter/submit only assessments assigned to their active linked faculty record.

1. Create `/policies`: programme/session, sourceAcademicYear/sourceReference, failGrade and 1–20 explicit grade bands. Bands start at 0%, have distinct thresholds/grade codes and nondecreasing points. No official grading values are seeded.
2. Another administrator approves `/policies/{id}/approve`, version/reason; academic year must match the session. Approved policies are immutable.
3. Create an assessment: subject, matching approved policy, faculty, section, scheduled date, configuration reference, credits and internal/external/pass limits. Saves an exact enrollment roster with Unentered marks.
4. `PUT /{id}/marks` supplies the entire distinct saved roster with version. Present requires both marks in range; Absent/Unentered requires empty marks. Fractional values have at most two decimals. Concurrent changes allow one version winner.
5. `POST /{id}/submit`, version/reason, requires every roster member entered and the scheduled IST date reached. Another administrator approves `/approve`. Explicit `/publish` makes the frozen result visible to portals. Locked stages reject edits and repeated transitions.

Totals and component pass checks use actual marks. Percentages/GPA round to two decimals, midpoint away from zero. A rounded display percentage cannot rescue an actual failing total. Failed subjects contribute zero points and remain in the GPA denominator. SGPA groups programme/session/semester; CGPA groups programme. Only published recorded subjects count. A unique published student/subject result prevents accidental double counting; retakes require a future approved attempt policy and cannot currently overwrite results.

`GET /students/{id}/results` is Manage-only. `/portal/students/{id}/results` checks an active student/guardian link. Both exclude draft/approved-but-unpublished marks. Academic results do not establish complete curriculum coverage or graduation eligibility on their own.

Remaining: practical-specific exams, assignment components, exam registrations/admit cards, moderation/reopening/revaluation, retakes and full official university integrations. An assessment date/list is not a complete examination calendar.
