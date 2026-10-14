let token='';
export class ApiError extends Error{constructor(public status:number,message:string){super(message);}}
export async function csrf(){const r=await fetch('/api/v1/auth/csrf',{credentials:'same-origin'});if(!r.ok)throw new ApiError(r.status,'Session could not be initialized.');token=(await r.json()).token;}
export async function api<T>(url:string,options:RequestInit={}):Promise<T>{
 const method=options.method??'GET';if(!['GET','HEAD'].includes(method)&&!token)await csrf();
 const r=await fetch('/api/v1'+url,{...options,credentials:'same-origin',headers:{'Content-Type':'application/json',...(token?{'X-CSRF-TOKEN':token}:{}),...options.headers}});
 if(!r.ok){let body;try{body=await r.json();}catch{}const validation=Object.values(body?.errors??{}).flat().join(' ');throw new ApiError(r.status,body?.detail||validation||(r.status===401?'Please sign in.':'Request failed.'));}return r.status===204?undefined as T:r.json();
}
export async function login(email:string,password:string,code?:string){await csrf();const result=await api<{requiresMfa:boolean}>(code?'/auth/mfa/login':'/auth/login',{method:'POST',body:JSON.stringify(code?{code}:{email,password})});await csrf();return result;}
