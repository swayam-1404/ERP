import fs from 'node:fs';
const root = new URL('../', import.meta.url);
const file = new URL('docs/research/verified_institution_data.json', root);
const data=JSON.parse(fs.readFileSync(file,'utf8'));
data.status='ACCEPTED_FOR_LOCAL_IMPLEMENTATION';
data.reference_resolution={date:'2026-10-14',authorization:'User: make erp by removing conflicts',rules:[
 'Preserve institution-specific published addresses; never overwrite them with group postcode.',
 'Prefer VIT own website programme nomenclature for VIT seed; do not seed AI & ML from conflicting central directory.',
 'VHSS remains outside initial institution inventory; common campus does not establish governance.',
 'Reference advertisements do not establish current approval, intake, fee rules or certificate authority.',
 'No privileged accounts derived from public professional directory.'
]};
fs.writeFileSync(file,JSON.stringify(data,null,2)+'\n');
fs.writeFileSync(new URL('docs/research/CONFLICT_RESOLUTIONS.md',root),`# Reference conflict resolutions\n\n2026-10-14: user instructed implementation and conflict resolution. This accepts Phase 0 for local development.\n\n| Conflict | Working decision | Evidence preserved |\n|---|---|---|\n| Group/VIT postcode | Keep source-specific addresses. VIT uses its own 768028; group address retains 768040. | S001 and S004; no claimed authoritative correction |\n| VIT branch names | Use VIT own CSE, EEE, Civil, Mechanical and MBA advertisements; do not activate AI & ML. | S004; central discrepancy remains recorded |\n| VHSS membership | Exclude from initial operational scope. | S030–S034 candidate reference retained |\n| Undated programme approval | Seed reference names with provenance; current intake and official academic policies unset. | Source scope preserved |\n| Missing fees/grading/signatures | Use explicitly DEMO configuration in development only; official functions fail closed. | No invented official rates/rules/authority |\n\nThese decisions remove ambiguous runtime configuration without deleting evidence or pretending the underlying institutional disagreements are resolved.\n`);
console.log('Reference configuration conflicts resolved; source evidence preserved.');
