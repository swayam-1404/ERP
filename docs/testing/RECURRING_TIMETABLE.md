# Recurring timetable increment — 2026-10-17

Implemented: weekly date expansion in explicit IST, multiple selected weekdays, manual skip dates, current conflict preview, atomic creation of template/dated classes/rosters/audits, scoped template listing/details, versioned cancellation of the future remainder, and class date filtering. RecurringTimetable migration adds TimetableSeries and an optional composite institution-scoped class-series foreign key. The same preparation code serves individual and series scheduling.

Backend coverage: preview UTC conversion and skips without persistence; saved roster counts; stale preview/middle-occurrence conflict with complete rollback of template/classes/rosters/audits; concurrent series creation with one winner; invalid duration/range/day/exclusion/occurrence limits; retained past submitted attendance after future cancellation; stale/replayed cancellation; faculty and institution denial; foreign resource, capacity, empty roster and CSRF rejection; scoped date-filtered class linkage.

Browser coverage: preview/edit invalidation, skipped occurrence, persisted series creation, future cancellation, class date filters, real booking conflict preview that disables creation, and mobile page-width checks. Preview headings receive focus so results become visible inside long dialogs. Screenshots: weekly-preview.png, weekly-cancelled.png and weekly-mobile-conflict.png.

Limitations: practical/lab subgroups, template editing/rescheduling, official holidays/shortage policies and student/guardian views remain pending. Later enrollment changes do not silently alter saved class rosters. Series scheduling does not approve curricula or official term dates. No production deployment, performance/load measurement or external notification.

Execution: 46 backend tests passed, 11 browser tests passed, frontend production build passed and 16 discovery checks passed. After the preview-focus improvement, both affected browser tests passed again. Full counts/outcomes and local raw result paths are recorded in TEST_RESULTS.md.
