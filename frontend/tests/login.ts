import {expect,test,type Page} from '@playwright/test';
import fs from 'node:fs';

const password=fs.readFileSync(new URL('../../.env',import.meta.url),'utf8').match(/^Seed__AdminPassword=(.+)$/m)![1].trim();

export async function signIn(page:Page,email='admin@example.test',options:{password?:string;heading?:string}={}){
 await page.goto('/');await page.getByLabel('Email address').fill(email);await page.getByLabel('Password',{exact:true}).fill(options.password??password);
 const deadline=Date.now()+65000;let extended=false;
 while(true){
  const pending=page.waitForResponse(r=>r.url().endsWith('/api/v1/auth/login')&&r.request().method()==='POST');
  await page.getByRole('button',{name:'Sign in',exact:true}).click();const response=await pending;
  if(response.status()!==429){expect(response.status()).toBe(200);break;}
  // Growing browser suites share the real server's fixed-window quota. Wait only
  // for explicit rate limiting; never bypass it or retry authentication errors.
  if(!extended){test.setTimeout(test.info().timeout+65000);extended=true;}
  if(Date.now()>=deadline)throw new Error('Login rate limit did not clear within its fixed window.');
  await page.waitForTimeout(5000);
 }
 await expect(page.getByRole('heading',{name:options.heading??(email==='student@example.test'?'Your campus journey.':email==='guardian@example.test'?'Your student’s campus journey.':email.startsWith('faculty')?'Your academic workspace.':'A clearer view of your campus.')})).toBeVisible();
}
