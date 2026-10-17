import {test,expect} from '@playwright/test';
import {signIn} from './login';
test('login, API-connected institution selection and department audit',async({page})=>{
 await signIn(page);
 await expect(page.getByRole('heading',{name:'A clearer view of your campus.'})).toBeVisible();
 expect((await page.request.get('/health/ready')).status()).toBe(200);
 await page.getByRole('link',{name:'Academic structure',exact:true}).click();
 await expect(page.getByText('B.Tech Electrical & Electronics Engineering',{exact:true})).toBeVisible();
 const name='DEMO Browser Department '+Date.now();await page.getByLabel('New department').fill(name);await page.getByRole('button',{name:'Add department'}).click();await expect(page.getByText(name,{exact:true})).toBeVisible();
 await page.getByRole('link',{name:'Audit trail',exact:true}).click();await expect(page.getByText('Department.Created',{exact:true}).first()).toBeVisible();
 await page.getByLabel('Select institution').selectOption({label:'Vikash Degree College'});await page.getByRole('link',{name:'Academic structure',exact:true}).click();await expect(page.getByText('M.A (English)',{exact:true})).toBeVisible();
 await page.getByRole('button',{name:'Sign out'}).click();await expect(page.getByRole('heading',{name:'Welcome back.'})).toBeVisible();
});
