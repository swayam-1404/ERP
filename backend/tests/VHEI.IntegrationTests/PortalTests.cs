using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VHEI.Api;
using VHEI.Infrastructure;
using Xunit;
namespace VHEI.IntegrationTests;
[CollectionDefinition("PostgresPortal")]public class PortalCollection:ICollectionFixture<CoreFixture>{}
[Collection("PostgresPortal")]
public class PortalTests(CoreFixture fixture)
{
    private string Root(Guid? institution=null)=>$"/api/v1/institutions/{institution??fixture.Vit}";
    private async Task<(Guid Id,string Email,string GuardianEmail)> Student(HttpClient admin,string? guardianEmail=null)
    {
        using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();var key=Guid.NewGuid().ToString("N");var email="student-"+key+"@example.test";guardianEmail??="guardian-"+key+"@example.test";
        var r=new StudentRequest("P-"+key,"DEMO Portal Student",email,"Active",await db.Programmes.Where(x=>x.InstitutionId==fixture.Vit).Select(x=>x.Id).FirstAsync(),await db.AcademicSessions.Where(x=>x.InstitutionId==fixture.Vit).Select(x=>x.Id).FirstAsync(),2026,1,"A","DEMO Portal Guardian",guardianEmail);
        var response=await admin.PostAsJsonAsync(Root()+"/students",r);Assert.Equal(HttpStatusCode.Created,response.StatusCode);return((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid(),email,guardianEmail);
    }
    private async Task<JsonElement> Grant(HttpClient admin,Guid student,string kind,string email)=>await(await admin.PostAsJsonAsync(Root()+$"/portal-access/students/{student}",new PortalAccountRequest(kind,email,CoreFixture.Password,"DEMO identity/relationship evidence"))).Content.ReadFromJsonAsync<JsonElement>();
    [Fact]public async Task SeededStudentAndGuardianSeeOnlyTheirLinkedRecord()
    {
        using var student=await fixture.Login("student@example.test");using var guardian=await fixture.Login("guardian@example.test");var s=await student.GetFromJsonAsync<JsonElement>(Root()+"/portal/students");var g=await guardian.GetFromJsonAsync<JsonElement>(Root()+"/portal/students");Assert.Equal(1,s.GetArrayLength());Assert.Equal(s[0].GetProperty("id").GetGuid(),g[0].GetProperty("id").GetGuid());
        using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();var id=s[0].GetProperty("id").GetGuid();var other=await db.Students.Where(x=>x.InstitutionId==fixture.Vit&&x.Id!=id).Select(x=>x.Id).FirstAsync();Assert.Equal(HttpStatusCode.OK,(await student.GetAsync(Root()+$"/portal/students/{id}")).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await student.GetAsync(Root()+$"/portal/students/{other}")).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await guardian.GetAsync(Root()+$"/portal/students/{other}/fees")).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await student.GetAsync(Root(fixture.Vdc)+"/portal/students")).StatusCode);
        var count=await db.PortalAccesses.CountAsync();await PortalDemoSeed.RunAsync(db);Assert.Equal(count,await db.PortalAccesses.CountAsync());
    }
    [Fact]public async Task PortalRolesCannotAccessAdministrativeTeachingOrFinanceData()
    {
        using var c=await fixture.Login("student@example.test");foreach(var path in new[]{"/students","/faculty","/admissions","/finance/charges","/academic/configuration","/audit","/portal-access"})Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync(Root()+path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync(Root()+"/portal-access/students/"+Guid.NewGuid(),new PortalAccountRequest("Guardian","nobody@example.test",CoreFixture.Password,"Try unauthorized grant"))).StatusCode);
        using var admin=await fixture.Login("admin@example.test");Assert.Equal(HttpStatusCode.Forbidden,(await admin.GetAsync(Root()+"/portal/students")).StatusCode);
    }
    [Fact]public async Task ProvisionedAccountMustChangePasswordBeforeOpeningRecords()
    {
        using var admin=await fixture.Login("institution@example.test");var s=await Student(admin);var access=await Grant(admin,s.Id,"Student",s.Email);Assert.True(access.GetProperty("mustChangePassword").GetBoolean());using var c=await fixture.Login(s.Email);
        var me=await c.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");Assert.True(me.GetProperty("mustChangePassword").GetBoolean());Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync(Root()+"/portal/students")).StatusCode);Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync("/api/v1/auth/password/change",new PasswordChangeRequest("wrong",CoreFixture.Password+"Changed"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await c.PostAsJsonAsync("/api/v1/auth/password/change",new PasswordChangeRequest(CoreFixture.Password,CoreFixture.Password+"Changed"))).StatusCode);Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync(Root()+"/portal/students")).StatusCode);
        await CoreFixture.Csrf(c);(await c.PostAsJsonAsync("/api/v1/auth/login",new{email=s.Email,password=CoreFixture.Password+"Changed"})).EnsureSuccessStatusCode();await CoreFixture.Csrf(c);var rows=await c.GetFromJsonAsync<JsonElement>(Root()+"/portal/students");Assert.Equal(s.Id,rows[0].GetProperty("id").GetGuid());
    }
    [Fact]public async Task RevocationTakesEffectForExistingSessionAndRetainsHistory()
    {
        using var admin=await fixture.Login("institution@example.test");var s=await Student(admin);var access=await Grant(admin,s.Id,"Student",s.Email);using var c=await fixture.Login(s.Email);(await c.PostAsJsonAsync("/api/v1/auth/password/change",new PasswordChangeRequest(CoreFixture.Password,CoreFixture.Password+"Changed"))).EnsureSuccessStatusCode();await CoreFixture.Csrf(c);(await c.PostAsJsonAsync("/api/v1/auth/login",new{email=s.Email,password=CoreFixture.Password+"Changed"})).EnsureSuccessStatusCode();
        var id=access.GetProperty("id").GetGuid();Assert.Equal(HttpStatusCode.NoContent,(await admin.PostAsJsonAsync(Root()+$"/portal-access/{id}/revoke",new ArchiveRequest(1,"Verified relationship withdrawn"))).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync(Root()+$"/portal/students/{s.Id}")).StatusCode);Assert.Equal(HttpStatusCode.Conflict,(await admin.PostAsJsonAsync(Root()+$"/portal-access/{id}/revoke",new ArchiveRequest(1,"Replay"))).StatusCode);
        using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();Assert.True(await db.PortalAccesses.AnyAsync(x=>x.Id==id&&!x.IsActive));Assert.True(await db.AuditLogs.AnyAsync(x=>x.Action=="PortalAccess.Revoked"&&x.Resource==id.ToString()));
    }
    [Fact]public async Task MatchingEmailAndRoleAreRequiredWithoutPrivilegeEscalation()
    {
        using var admin=await fixture.Login("institution@example.test");var s=await Student(admin);
        Assert.Equal(HttpStatusCode.BadRequest,(await admin.PostAsJsonAsync(Root()+$"/portal-access/students/{s.Id}",new PortalAccountRequest("Student","foreign@example.test",CoreFixture.Password,"Wrong email"))).StatusCode);Assert.Equal(HttpStatusCode.BadRequest,(await admin.PostAsJsonAsync(Root()+$"/portal-access/students/{s.Id}",new PortalAccountRequest("CentralAdministrator",s.Email,CoreFixture.Password,"Try escalation"))).StatusCode);
        using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();var student=await db.Students.SingleAsync(x=>x.Id==s.Id);student.Email="admin@example.test";await db.SaveChangesAsync();Assert.Equal(HttpStatusCode.Conflict,(await admin.PostAsJsonAsync(Root()+$"/portal-access/students/{s.Id}",new PortalAccountRequest("Student","admin@example.test",null,"Incompatible privileged identity"))).StatusCode);
        Assert.False(await db.PortalAccesses.AnyAsync(x=>x.StudentId==s.Id));
    }
    [Fact]public async Task GuardianCanHaveTwoExplicitWardsButCannotInferAnother()
    {
        using var admin=await fixture.Login("institution@example.test");var one=await Student(admin);var two=await Student(admin,one.GuardianEmail);await Grant(admin,one.Id,"Guardian",one.GuardianEmail);await Grant(admin,two.Id,"Guardian",one.GuardianEmail);
        using var c=await fixture.Login(one.GuardianEmail);(await c.PostAsJsonAsync("/api/v1/auth/password/change",new PasswordChangeRequest(CoreFixture.Password,CoreFixture.Password+"Changed"))).EnsureSuccessStatusCode();await CoreFixture.Csrf(c);(await c.PostAsJsonAsync("/api/v1/auth/login",new{email=one.GuardianEmail,password=CoreFixture.Password+"Changed"})).EnsureSuccessStatusCode();var rows=await c.GetFromJsonAsync<JsonElement>(Root()+"/portal/students");Assert.Equal(2,rows.GetArrayLength());Assert.Contains(rows.EnumerateArray(),x=>x.GetProperty("id").GetGuid()==one.Id);Assert.Contains(rows.EnumerateArray(),x=>x.GetProperty("id").GetGuid()==two.Id);
    }
    [Fact]public async Task ArchivedStudentsForeignResourcesAndMissingCsrfStayProtected()
    {
        using var admin=await fixture.Login("institution@example.test");var s=await Student(admin);Assert.Equal(HttpStatusCode.NotFound,(await admin.PostAsJsonAsync(Root()+$"/portal-access/students/{Guid.NewGuid()}",new PortalAccountRequest("Student",s.Email,CoreFixture.Password,"Foreign identifier"))).StatusCode);(await admin.PostAsJsonAsync(Root()+$"/students/{s.Id}/archive",new ArchiveRequest(1,"Archive portal fixture"))).EnsureSuccessStatusCode();Assert.Equal(HttpStatusCode.Conflict,(await admin.PostAsJsonAsync(Root()+$"/portal-access/students/{s.Id}",new PortalAccountRequest("Student",s.Email,CoreFixture.Password,"Archived account request"))).StatusCode);
        admin.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");Assert.Equal(HttpStatusCode.BadRequest,(await admin.PostAsJsonAsync(Root()+$"/portal-access/students/{s.Id}",new PortalAccountRequest("Student",s.Email,CoreFixture.Password,"Missing CSRF"))).StatusCode);
    }
}
