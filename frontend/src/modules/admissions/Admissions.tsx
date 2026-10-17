import {useState} from 'react';
import {useQuery,useQueryClient} from '@tanstack/react-query';
import {api} from '../../services/api';
import {Button} from '../../components/ui/button';
import {Dialog} from '../../components/ui/dialog';
import type {Reference} from '../../types/core';

type Application={id:string;applicationNumber:string;fullName:string;email:string;programmeId:string;academicSessionId:string;externalReference:string;status:string;version:number;submittedBy:string|null;policyReference:string;eligibilityNote:string;documentReference:string;reviewNote:string};
type Detail={application:Application;history:{id:string;status:string;note:string;occurredAt:string}[]};
const statuses=['Draft','Submitted','Reviewed','Rejected','Cancelled'];

export function Admissions({institution,userId}:{institution:string;userId:string}){
 const client=useQueryClient(),base=`/institutions/${institution}/admissions`;
 const [search,setSearch]=useState(''),[status,setStatus]=useState(''),[page,setPage]=useState(1),[creating,setCreating]=useState(false),[selected,setSelected]=useState('');
 const reference=useQuery({queryKey:['reference',institution],queryFn:()=>api<Reference>(`/institutions/${institution}/reference`)});
 const q=useQuery({queryKey:['admissions',institution,'list',search,status,page],queryFn:()=>api<{items:Application[];total:number}>(`${base}?page=${page}&search=${encodeURIComponent(search)}${status?'&status='+status:''}`)});
 const summary=useQuery({queryKey:['admissions',institution,'summary'],queryFn:()=>api<{status:string;count:number}[]>(`${base}/summary`)});
 const refresh=()=>client.invalidateQueries({queryKey:['admissions',institution]});
 return <><div className="page-title"><div><p className="eyebrow">ADMISSIONS</p><h1>Every application, accounted for.</h1><p>Register applicants, track evidence and record independent review.</p></div><Button disabled={!reference.data?.programmes.length||!reference.data.sessions.length} onClick={()=>setCreating(true)}>New application</Button></div>
 <p className="admission-notice">Reviewed means the recorded evidence was checked by another administrator. Admission approval, fee confirmation and enrolment are still pending separate workflows.</p>
 <div className="admission-counts" aria-label="Application totals">{statuses.map(s=><button key={s} className={status===s?'selected':''} onClick={()=>{setStatus(status===s?'':s);setPage(1)}}><strong>{summary.data?.find(x=>x.status===s)?.count??0}</strong><span>{s}</span></button>)}</div>
 <section className="panel"><div className="panel-heading"><h2>Applications</h2><span className="muted">{q.data?.total??0} matching records</span></div><div className="admission-filters"><label>Search applications<input maxLength={100} placeholder="Name, application or counselling reference" value={search} onChange={e=>{setSearch(e.target.value);setPage(1)}}/></label><label>Application status<select aria-label="Application status" value={status} onChange={e=>{setStatus(e.target.value);setPage(1)}}><option value="">All statuses</option>{statuses.map(s=><option key={s}>{s}</option>)}</select></label></div>
 {(q.error||reference.error||summary.error)&&<p role="alert" className="error">{(q.error||reference.error||summary.error)?.message}</p>}{q.isPending&&<p className="state">Loading applications…</p>}
 <div className="table-scroll"><table><thead><tr><th>Application</th><th>Applicant</th><th>Programme</th><th>Status</th><th>Action</th></tr></thead><tbody>{q.data?.items.map(a=><tr key={a.id}><td><strong>{a.applicationNumber}</strong><small className="cell-sub">{a.externalReference||'No counselling reference'}</small></td><td>{a.fullName}</td><td>{reference.data?.programmes.find(p=>p.id===a.programmeId)?.name??'Loading…'}</td><td><span className="badge quiet">{a.status}</span></td><td><Button className="secondary" onClick={()=>setSelected(a.id)}>View application</Button></td></tr>)}</tbody></table></div>{q.data?.items.length===0&&<p className="state">No applications match these filters.</p>}
 <div className="pagination"><span>Page {page}</span><Button className="secondary" disabled={page===1} onClick={()=>setPage(p=>p-1)}>Previous</Button><Button className="secondary" disabled={!q.data||page*25>=q.data.total} onClick={()=>setPage(p=>p+1)}>Next</Button></div></section>
 {creating&&reference.data&&<ApplicationEditor base={base} reference={reference.data} onClose={()=>setCreating(false)} onSaved={async()=>{await refresh();setCreating(false)}}/>}
 {selected&&reference.data&&<ApplicationDetail key={selected} base={base} institution={institution} id={selected} reference={reference.data} userId={userId} onClose={()=>setSelected('')} refresh={refresh}/>}
 </>;
}

function ApplicationEditor({base,reference,profile,onClose,onSaved}:{base:string;reference:Reference;profile?:Application;onClose:()=>void;onSaved:()=>Promise<void>}){
 const [error,setError]=useState(''),[busy,setBusy]=useState(false);
 return <Dialog open onOpenChange={v=>{if(!v&&!busy)onClose()}} title={profile?'Edit application draft':'Register application'} description="Registration saves a draft. It does not create a student account or enrolment."><form onSubmit={async e=>{e.preventDefault();const f=new FormData(e.currentTarget);setBusy(true);setError('');try{await api(base+(profile?'/'+profile.id:''),{method:profile?'PUT':'POST',body:JSON.stringify({...Object.fromEntries(f),version:profile?.version??1})});await onSaved();}catch(e){setError((e as Error).message)}finally{setBusy(false)}}}>
 <label>Application number<input name="applicationNumber" required maxLength={40} pattern="[A-Za-z0-9/-]+" defaultValue={profile?.applicationNumber} placeholder="DEMO-APP-001"/></label>
 <label>Applicant name<input name="fullName" required minLength={2} maxLength={120} defaultValue={profile?.fullName}/></label>
 <label>Applicant email<input name="email" type="email" required maxLength={254} defaultValue={profile?.email}/></label>
 <label>Programme<select name="programmeId" aria-label="Programme" required defaultValue={profile?.programmeId??reference.programmes[0]?.id}>{reference.programmes.map(p=><option key={p.id} value={p.id}>{p.name}</option>)}</select></label>
 <label>Academic session<select name="academicSessionId" aria-label="Academic session" required defaultValue={profile?.academicSessionId??reference.sessions[0]?.id}>{reference.sessions.map(s=><option key={s.id} value={s.id}>{s.name}{s.isDemo?' (DEMO)':''}</option>)}</select></label>
 <label>External counselling reference (optional)<input name="externalReference" maxLength={120} defaultValue={profile?.externalReference??''}/></label>
 {error&&<p role="alert" className="error">{error}</p>}<div className="dialog-actions"><Button disabled={busy}>{busy?'Saving…':'Save draft'}</Button></div></form></Dialog>;
}

function ApplicationDetail({base,institution,id,reference,userId,onClose,refresh}:{base:string;institution:string;id:string;reference:Reference;userId:string;onClose:()=>void;refresh:()=>Promise<void>}){
 const q=useQuery({queryKey:['admissions',institution,'detail',id],queryFn:()=>api<Detail>(`${base}/${id}`)});
 const [editing,setEditing]=useState(false),[action,setAction]=useState(''),[reason,setReason]=useState(''),[error,setError]=useState(''),[busy,setBusy]=useState(false);
 const a=q.data?.application,canReview=a?.status==='Submitted'&&a.submittedBy!==userId;
 const actions=a?.status==='Draft'?['Submit','Cancel']:a?.status==='Submitted'?[...(canReview?['Review','Reject']:[]),'Cancel']:a?.status==='Reviewed'?['Cancel']:[];
 return <><Dialog open onOpenChange={v=>{if(!v&&!busy)onClose()}} title="Application details" description="Application history is retained. Evidence references point to records verified outside this workspace.">
 {q.isPending&&<p>Loading application…</p>}{q.error&&<p role="alert" className="error">{q.error.message}</p>}{a&&<><h3>{a.fullName}</h3><dl className="admission-details"><dt>Application</dt><dd>{a.applicationNumber}</dd><dt>Email</dt><dd>{a.email}</dd><dt>Status</dt><dd>{a.status} · version {a.version}</dd><dt>Programme</dt><dd>{reference.programmes.find(p=>p.id===a.programmeId)?.name}</dd><dt>Session</dt><dd>{reference.sessions.find(s=>s.id===a.academicSessionId)?.name}</dd><dt>Counselling reference</dt><dd>{a.externalReference||'Not recorded'}</dd>{a.reviewNote&&<><dt>Review note</dt><dd>{a.reviewNote}</dd></>}{a.policyReference&&<><dt>Policy reference</dt><dd>{a.policyReference}</dd><dt>Eligibility evidence</dt><dd>{a.eligibilityNote}</dd><dt>Verified documents</dt><dd>{a.documentReference}</dd></>}</dl>
 {a.status==='Draft'&&<Button className="secondary" onClick={()=>setEditing(true)}>Edit draft</Button>}
 {a.status==='Submitted'&&!canReview&&<p className="admission-notice">Another administrator must review this submission.</p>}
 {a.status==='Reviewed'&&<p className="admission-notice">Evidence review recorded. Fee confirmation and admission approval are pending. No student has been enrolled from this application.</p>}
 {actions.length>0&&<form className="admission-action" onSubmit={async e=>{e.preventDefault();const f=new FormData(e.currentTarget);setBusy(true);setError('');try{await api(`${base}/${id}/transition`,{method:'POST',body:JSON.stringify({...Object.fromEntries(f),action,reason,version:a.version})});setAction('');setReason('');await refresh();}catch(e){setError((e as Error).message)}finally{setBusy(false)}}}>
 <label>Application action<select aria-label="Application action" required value={action} onChange={e=>setAction(e.target.value)}><option value="">Choose action</option>{actions.map(s=><option key={s} value={s}>{s==='Review'?'Complete evidence review':s==='Reject'?'Reject application':s==='Cancel'?'Cancel application':'Submit for review'}</option>)}</select></label>
 {action==='Review'&&<><label>Applicable policy reference<input name="policyReference" required minLength={3} maxLength={500} placeholder="Approved policy/version and record reference"/></label><label>Eligibility evidence<textarea name="eligibilityNote" required minLength={3} maxLength={1000}/></label><label>Verified document reference<textarea name="documentReference" required minLength={3} maxLength={1000} placeholder="Identify records checked and where they are held"/></label></>}
 {action&&<><label>Action note<textarea required minLength={3} maxLength={1000} value={reason} onChange={e=>setReason(e.target.value)}/></label><Button disabled={busy}>{busy?'Saving…':'Record action'}</Button></>}{error&&<p role="alert" className="error">{error}</p>}</form>}
 <h3>Application history</h3><ol className="admission-history">{q.data?.history.map(h=><li key={h.id}><strong>{h.status}</strong> <time>{new Date(h.occurredAt).toLocaleString('en-IN',{timeZone:'Asia/Kolkata'})}</time><p>{h.note}</p></li>)}</ol></>}
 </Dialog>{editing&&a&&<ApplicationEditor base={base} reference={reference} profile={a} onClose={()=>setEditing(false)} onSaved={async()=>{await refresh();setEditing(false)}}/>}</>;
}
