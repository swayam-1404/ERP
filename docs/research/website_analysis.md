# Website analysis

Investigation date: 2026-10-14 (Asia/Calcutta). Low-volume public reads only. See source_register.csv for exact URLs, methods, titles, temporal labels and failed attempts. No authentication, private portals, student directory, contact submissions or student-data PDFs were accessed. Search may return student lists; those were deliberately excluded.

## VHEI

https://vheibargarh.in/ returned HTTP 200 but only a React shell to the text reader. A rendered browser exposed its directory, contact footer, admissions and leadership sections. The admissions page was inspected at https://vheibargarh.in/admissions. The directory publishes eight higher institutions, seven website links and a coming-soon paramedical listing. Public marketing totals were excluded from ERP data. The page displays logos and copyright notices; store asset references only. robots.txt returned the same HTML shell, not valid crawler directives. No explicit terms page was observed in the inspected navigation; this is not a claim that terms do not exist.

## VIT

https://vitbargarh.ac.in/ is also JavaScript-driven. Homepage, departments and academic-calendar pages were inspected in a rendered browser. The department page identifies six departments; the homepage advertises four B.Tech branches and MBA. Source records retain published leadership, office contacts and facilities. Central directory wording differs from the VIT branch list and postcode. Staff/faculty, placement and IQAC links were observed but their full bodies were not inspected.

Mandatory disclosure, one historical calendar and exam-registration PDF failed or were not parsed by the web reader. HTTP retries returned application/pdf with positive lengths. This establishes availability, not inspected contents, sanction validity, signatures or rules. Other calendar/form links were observed but not fetched. No copied PDFs are included. robots.txt returned HTML fallback.

## VDC

https://vdcbargarh.ac.in/ exposes traditional navigation with UG departments, PG offerings, staff, notices, facilities and quality-assurance pages. Admission guidelines, teaching directory, contact, college rules, scholarship, hostel, training/placement, IQAC, SSR and notice pages were read. Physics and MA English detail links were fetched; their detailed curriculum was not adopted. Programmes and calendar detail routes returned 404. Rules/scholarship/hostel bodies are image-based, so substantive rules remain unverified. IQAC/SSR extracted tables had no document rows; no accreditation conclusion was drawn. Historical notices reference examination registration, schedules and pass-certificate collection. A certificate-notice attachment link resolved to a PDF icon image; contents were not established. robots.txt explicitly allows public crawling.

## Linked subdomains

VSBM, VPT, VLS, VNC and VPC linked hosts returned HTTP 200 with the common VHEI title and shell. VSBM/VLS initially failed in the web reader and succeeded on direct HTTP retry. Separate rendered institution content was not inspected, so these are linked websites with S001 membership evidence, not independently corroborated programme/approval records.

## Higher-secondary candidate

Official About and Academics pages at https://vhssbgh.krishnavikash.com/ were read. They publish Science/Commerce, CHSE context and the Knowledge Hub location while using Krishna Vikash group identity. A historical CHSE district PDF is an additional identity reference, not proof of ERP governance. Admission/contact search excerpts were recorded separately from fully inspected pages. VHSS remains outside automatic VHEI seeds until membership confirmation.

## Next research pass

Obtain current approval letters, academic regulations, fee schedules, authorized certificate templates, current faculty assignment lists and permission for brand assets from institutional administrators. Review PDF contents and image-based rules with visual extraction before converting any policy into executable rules.
