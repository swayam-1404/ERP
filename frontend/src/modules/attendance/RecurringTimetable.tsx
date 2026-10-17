import {useState,useEffect,useRef} from 'react';
import {useQuery,useQueryClient} from '@tanstack/react-query';
import {api,ApiError} from '../../services/api';
import {Button} from '../../components/ui/button';
import {Dialog} from '../../components/ui/dialog';

type Config={subjects:{id:string;name:string;code:string;semester:number}[];faculty:{id:string;fullName:string}[];classrooms:{id:string;name:string;capacity:number}[]};
type Series={id:string;name:string;firstDate:string;lastDate:string;startTime:string;endTime:string;daysOfWeek:number[];isCancelled:boolean;version:number;periodCount:number;cancelledCount:number};
type Period={id?:string;startsAt:string;endsAt:string;conflicts?:string[];isCancelled?:boolean;attendanceSubmitted?:boolean;rosterCount?:number};
type Preview={periods:Period[];rosterCount:number;hasConflicts:boolean};
const days=['Sunday','Monday','Tuesday','Wednesday','Thursday','Friday','Saturday'];
const format=(s:string)=>new Date(s).toLocaleString('en-IN',{timeZone:'Asia/Kolkata',year:'numeric',month:'short',day:'2-digit',hour:'2-digit',minute:'2-digit'});
const date=(s:string)=>new Date(s+'T00:00:00+05:30').toLocaleDateString('en-IN',{timeZone:'Asia/Kolkata',year:'numeric',month:'short',day:'2-digit'});

export function RecurringTimetable({institution,config,onRefresh}:{institution:string;config:Config;onRefresh:()=>Promise<void>}){
 const client=useQueryClient(),base=`/institutions/${institution}/academic/series`;
 const [creating,setCreating]=useState(false),[selected,setSelected]=useState(''),[page,setPage]=useState(1);
 const q=useQuery({queryKey:['timetable-series',institution,page],queryFn:()=>api<{items:Series[];total:number}>(`${base}?page=${page}`)});
 const refresh=async()=>{await client.invalidateQueries({queryKey:['timetable-series',institution]});await onRefresh()};
 return <><section className="panel"><div className="panel-heading"><div><h2>Weekly timetable series</h2><p className="muted">Preview each date before creating up to 64 class periods.</p></div><Button disabled={!config.subjects.length||!config.faculty.length||!config.classrooms.length} onClick={()=>setCreating(true)}>Schedule weekly series</Button></div>
 <p className="data-footnote">All times use IST. Enter dates to skip for holidays or other closures. Each period keeps the roster saved when the series is created.</p>
 {q.error&&<p role="alert" className="error">{q.error.message}</p>}{q.isPending&&<p className="state">Loading weekly series…</p>}
 <div className="table-scroll"><table><thead><tr><th>Series</th><th>Dates</th><th>Weekly pattern</th><th>Periods</th><th>Action</th></tr></thead><tbody>{q.data?.items.map(s=><tr key={s.id}><td><strong>{s.name}</strong><span className="cell-sub">{s.isCancelled?'Remaining periods cancelled':'Scheduled'}</span></td><td>{date(s.firstDate)} – {date(s.lastDate)}</td><td>{s.daysOfWeek.map(d=>days[d].slice(0,3)).join(', ')}<span className="cell-sub">{s.startTime.slice(0,5)}–{s.endTime.slice(0,5)} IST</span></td><td>{s.periodCount} total · {s.cancelledCount} cancelled</td><td><Button className="secondary" onClick={()=>setSelected(s.id)}>View series</Button></td></tr>)}</tbody></table></div>{q.data?.items.length===0&&<p className="state">No weekly series scheduled yet.</p>}
 <div className="pagination"><span>{q.data?.total??0} series · Page {page}</span><Button className="secondary" disabled={page===1} onClick={()=>setPage(p=>p-1)}>Previous</Button><Button className="secondary" disabled={!q.data||page*20>=q.data.total} onClick={()=>setPage(p=>p+1)}>Next</Button></div></section>
 {creating&&<SeriesEditor base={base} config={config} onClose={()=>setCreating(false)} onSaved={async()=>{await refresh();setCreating(false)}}/>}
 {selected&&<SeriesDetail key={selected} base={base} institution={institution} id={selected} onClose={()=>setSelected('')} refresh={refresh}/>}
 </>;
}

function SeriesEditor({base,config,onClose,onSaved}:{base:string;config:Config;onClose:()=>void;onSaved:()=>Promise<void>}){
 const [preview,setPreview]=useState<{result:Preview;payload:unknown}|null>(null),[error,setError]=useState(''),[busy,setBusy]=useState(false);
 const previewHeading=useRef<HTMLHeadingElement>(null);
 useEffect(()=>{if(preview)previewHeading.current?.focus()},[preview]);
 const first=new Date(Date.now()+330*60000).toISOString().slice(0,10),last=new Date(Date.now()+330*60000+28*86400000).toISOString().slice(0,10);
 return <Dialog open onOpenChange={v=>{if(!v&&!busy)onClose()}} title="Schedule weekly series" description="Select weekdays and review the generated dates. If any period conflicts, the entire series is rejected."><form onChange={()=>setPreview(null)} onSubmit={async e=>{e.preventDefault();const f=new FormData(e.currentTarget);const payload={name:f.get('name'),subjectId:f.get('subjectId'),facultyId:f.get('facultyId'),classroomId:f.get('classroomId'),section:f.get('section'),firstDate:f.get('firstDate'),lastDate:f.get('lastDate'),startTime:f.get('startTime')+':00',endTime:f.get('endTime')+':00',daysOfWeek:f.getAll('days').map(Number),excludedDates:String(f.get('excludedDates')??'').split(/[\s,]+/).filter(Boolean)};setBusy(true);setError('');setPreview(null);try{const result=await api<Preview>(`${base}/preview`,{method:'POST',body:JSON.stringify(payload)});setPreview({result,payload});}catch(e){setError((e as Error).message)}finally{setBusy(false)}}}>
 <fieldset disabled={busy} className="series-fields"><label>Series name<input name="name" required maxLength={120} placeholder="DEMO weekly mathematics"/></label>
 <label>Subject<select aria-label="Subject" name="subjectId" required>{config.subjects.map(s=><option key={s.id} value={s.id}>{s.code} · {s.name} · Semester {s.semester}</option>)}</select></label>
 <div className="form-grid"><label>Faculty<select aria-label="Faculty" name="facultyId" required>{config.faculty.map(f=><option key={f.id} value={f.id}>{f.fullName}</option>)}</select></label><label>Classroom<select aria-label="Classroom" name="classroomId" required>{config.classrooms.map(r=><option key={r.id} value={r.id}>{r.name} ({r.capacity} seats)</option>)}</select></label><label>Section<input name="section" defaultValue="A" required maxLength={12}/></label><label>First date<input type="date" name="firstDate" required defaultValue={first}/></label><label>Last date<input type="date" name="lastDate" required defaultValue={last}/></label><label>Start time (IST)<input type="time" name="startTime" required defaultValue="09:00"/></label><label>End time (IST)<input type="time" name="endTime" required defaultValue="10:00"/></label></div>
 <fieldset className="series-weekdays"><legend>Repeat on weekdays</legend>{days.map((d,i)=><label key={d}><input type="checkbox" name="days" value={i} defaultChecked={i===1}/>{d}</label>)}</fieldset>
 <label>Dates to skip (optional)<textarea name="excludedDates" placeholder="YYYY-MM-DD, separated by commas or new lines"/></label><p className="data-footnote">Choose a range up to 112 days and a same-day class lasting 1 minute to 4 hours. Skipped dates must match selected weekdays.</p>
 <Button className="secondary" disabled={busy}>{busy?'Checking dates…':'Preview dates'}</Button></fieldset></form>
 {error&&<p role="alert" className="error">{error}</p>}{preview&&<section className="series-preview"><h3 ref={previewHeading} tabIndex={-1}>Preview: {preview.result.periods.length} periods</h3><p>{preview.result.rosterCount} students per saved roster · IST</p><p role="status">{preview.result.hasConflicts?'Conflicts found. Change the schedule and preview again.':'No conflicts found at preview. Availability is checked again when saving.'}</p><PeriodTable periods={preview.result.periods} preview/>
 <div className="dialog-actions"><Button disabled={busy||preview.result.hasConflicts} onClick={async()=>{setBusy(true);setError('');try{await api(base,{method:'POST',body:JSON.stringify(preview.payload)});await onSaved()}catch(e){setError((e as Error).message);if(e instanceof ApiError&&e.status===409)setPreview(null)}finally{setBusy(false)}}}>{busy?'Saving…':'Create weekly series'}</Button></div></section>}
 </Dialog>;
}

function PeriodTable({periods,preview=false}:{periods:Period[];preview?:boolean}){return <div className="table-scroll series-periods"><table><thead><tr><th>Starts (IST)</th><th>Ends (IST)</th><th>{preview?'Availability':'Status'}</th></tr></thead><tbody>{periods.map(p=><tr key={p.id??p.startsAt}><td>{format(p.startsAt)}</td><td>{format(p.endsAt)}</td><td>{preview?(p.conflicts?.length?p.conflicts.join(', ')+' conflict':'Available'):p.isCancelled?'Cancelled':p.attendanceSubmitted?'Attendance recorded':'Scheduled'}</td></tr>)}</tbody></table></div>}

function SeriesDetail({base,institution,id,onClose,refresh}:{base:string;institution:string;id:string;onClose:()=>void;refresh:()=>Promise<void>}){
 const q=useQuery({queryKey:['timetable-series',institution,'detail',id],queryFn:()=>api<{series:Series&{excludedDates:string[]};periods:Period[]}>(`${base}/${id}`)});
 const [reason,setReason]=useState(''),[busy,setBusy]=useState(false),[error,setError]=useState('');const data=q.data;
 return <Dialog open onOpenChange={v=>{if(!v&&!busy)onClose()}} title="Weekly series details" description="Dated classes remain available in the class schedule. Cancelling the remainder preserves past classes, submitted attendance and all saved rosters.">
 {q.isPending&&<p className="state">Loading series…</p>}{q.error&&<p role="alert" className="error">{q.error.message}</p>}{data&&<><h3>{data.series.name}</h3><p>{date(data.series.firstDate)} – {date(data.series.lastDate)} · IST</p><p>Skipped dates: {data.series.excludedDates.length?data.series.excludedDates.map(date).join(', '):'None'}</p>{data.series.isCancelled&&<p role="status">Remaining future periods cancelled.</p>}<PeriodTable periods={data.periods}/>
 {!data.series.isCancelled&&data.periods.some(p=>!p.isCancelled&&!p.attendanceSubmitted&&new Date(p.startsAt).getTime()>Date.now())&&<form className="series-cancel" onSubmit={async e=>{e.preventDefault();setBusy(true);setError('');try{await api(`${base}/${id}/cancel`,{method:'POST',body:JSON.stringify({version:data.series.version,reason})});setReason('');await refresh()}catch(e){setError((e as Error).message)}finally{setBusy(false)}}}><label>Reason for cancelling remaining classes<textarea required minLength={3} maxLength={500} value={reason} onChange={e=>setReason(e.target.value)}/></label><Button disabled={busy}>{busy?'Cancelling…':'Cancel remaining future classes'}</Button></form>}{error&&<p role="alert" className="error">{error}</p>}</>}
 </Dialog>;
}

export function ScheduleRangeFilter({onApply}:{onApply:(range:{from:string;to:string})=>void}){
 const [error,setError]=useState('');
 return <form className="schedule-range" onSubmit={e=>{e.preventDefault();const f=new FormData(e.currentTarget);const from=new Date(f.get('from')+'T00:00:00+05:30'),to=new Date(f.get('to')+'T00:00:00+05:30');to.setUTCDate(to.getUTCDate()+1);if(!Number.isFinite(from.getTime())||!Number.isFinite(to.getTime())||to<=from||to.getTime()-from.getTime()>120*86400000){setError('Choose a date range up to 120 days.');return}setError('');onApply({from:from.toISOString(),to:to.toISOString()})}}><label>From date<input type="date" name="from" required/></label><label>Through date<input type="date" name="to" required/></label><Button className="secondary">Show dates</Button><Button type="button" className="secondary" onClick={()=>{setError('');onApply({from:'',to:''})}}>Reset dates</Button>{error&&<p role="alert" className="error">{error}</p>}</form>;
}
