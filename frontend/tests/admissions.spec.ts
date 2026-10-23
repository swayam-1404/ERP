import {test,expect} from '@playwright/test';
import {signIn} from './login';
async function login(page:import('@playwright/test').Page,email:string){
 await signIn(page,email);await page.getByRole('link',{name:'Admissions',exact:true}).click();
}

test('register, edit, submit, independently review and cancel an application with retained history',async({page})=>{
 test.setTimeout(120000);const number='DEMO-APP-'+Date.now();
 await login(page,'institution@example.test');await page.getByRole('button',{name:'New application',exact:true}).click();
 const form=page.getByRole('dialog',{name:'Register application',exact:true});
 await form.getByLabel('Application number',{exact:true}).fill(number);await form.getByLabel('Applicant name',{exact:true}).fill('DEMO Browser Applicant');await form.getByLabel('Applicant email',{exact:true}).fill('browser-applicant@example.test');await form.getByLabel('External counselling reference (optional)',{exact:true}).fill('DEMO-COUNSELLING-1');
 await form.getByRole('button',{name:'Save draft',exact:true}).click();await expect(form).not.toBeVisible();
 await page.getByLabel('Search applications',{exact:true}).fill(number);const row=page.getByRole('row').filter({hasText:number});await row.getByRole('button',{name:'View application',exact:true}).click();
 let detail=page.getByRole('dialog',{name:'Application details',exact:true});await detail.getByRole('button',{name:'Edit draft',exact:true}).click();const edit=page.getByRole('dialog',{name:'Edit application draft',exact:true});await edit.getByLabel('Applicant name',{exact:true}).fill('DEMO Revised Applicant');await edit.getByRole('button',{name:'Save draft',exact:true}).click();await expect(edit).not.toBeVisible();await expect(detail.getByRole('heading',{name:'DEMO Revised Applicant',exact:true})).toBeVisible();
 await detail.getByLabel('Application action',{exact:true}).selectOption('Submit');await detail.getByLabel('Action note',{exact:true}).fill('Fictional browser application ready for review');await detail.getByRole('button',{name:'Record action',exact:true}).click();await expect(detail.getByText('Another administrator must review this submission.',{exact:true})).toBeVisible();await expect(detail.getByRole('button',{name:'Edit draft',exact:true})).toHaveCount(0);await detail.getByRole('button',{name:'Close',exact:true}).click();
 await page.getByRole('button',{name:'Sign out',exact:true}).click();await expect(page.getByRole('heading',{name:'Welcome back.',exact:true})).toBeVisible();
 await login(page,'admin@example.test');await page.getByLabel('Search applications',{exact:true}).fill(number);await page.getByRole('row').filter({hasText:number}).getByRole('button',{name:'View application',exact:true}).click();detail=page.getByRole('dialog',{name:'Application details',exact:true});
 await detail.getByLabel('Application action',{exact:true}).selectOption('Review');await detail.getByLabel('Applicable policy reference',{exact:true}).fill('DEMO policy only; not an official rule');await detail.getByLabel('Eligibility evidence',{exact:true}).fill('Fictional evidence checked for browser testing');await detail.getByLabel('Verified document reference',{exact:true}).fill('DEMO document register entry 1');await detail.getByLabel('Action note',{exact:true}).fill('Independent demo evidence review completed');await detail.getByRole('button',{name:'Record action',exact:true}).click();await expect(detail.getByText('Evidence review recorded.',{exact:false})).toBeVisible();await expect(detail.locator('.admission-history li')).toHaveCount(4);
 await detail.getByLabel('Application action',{exact:true}).selectOption('Cancel');await detail.getByLabel('Action note',{exact:true}).fill('Fictional applicant withdrew after review');await detail.getByRole('button',{name:'Record action',exact:true}).click();await expect(detail.getByText('Cancelled · version 5',{exact:true})).toBeVisible();await expect(detail.locator('.admission-history li')).toHaveCount(5);await page.screenshot({path:'../docs/testing/screenshots/admission-history.png',fullPage:true});await detail.getByRole('button',{name:'Close',exact:true}).click();await expect(page.getByRole('row').filter({hasText:number}).getByText('Cancelled',{exact:true})).toBeVisible();await page.reload();await page.getByLabel('Search applications',{exact:true}).fill(number);await expect(page.getByRole('row').filter({hasText:number}).getByText('Cancelled',{exact:true})).toBeVisible();
 await page.screenshot({path:'../docs/testing/screenshots/admissions-desktop.png',fullPage:true});
});

test('admissions filters and empty state remain usable on mobile',async({page})=>{
 await login(page,'admin@example.test');await page.setViewportSize({width:390,height:844});
 await page.getByLabel('Search applications',{exact:true}).fill('NO-MATCH-'+Date.now());await expect(page.getByText('No applications match these filters.',{exact:true})).toBeVisible();await page.getByLabel('Application status',{exact:true}).selectOption('Submitted');
 expect(await page.evaluate(()=>document.documentElement.scrollWidth<=window.innerWidth)).toBe(true);
 await page.screenshot({path:'../docs/testing/screenshots/admissions-mobile.png',fullPage:true});
});
