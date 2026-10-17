using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VHEI.Api;
using VHEI.Infrastructure;
using Xunit;
namespace VHEI.IntegrationTests;

[Collection("PostgresCore")]
public class AdmissionTests(CoreFixture fixture)
{
    private string Path(Guid? institution=null)=>$"/api/v1/institutions/{institution??fixture.Vit}/admissions";
    private async Task<AdmissionRequest> Request(Guid? institution=null)
    {
        var tenant=institution??fixture.Vit;
        using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        return new("APP-"+Guid.NewGuid().ToString("N"),"Applicant Demo","applicant@example.test",await db.Programmes.Where(x=>x.InstitutionId==tenant).Select(x=>x.Id).FirstAsync(),await db.AcademicSessions.Where(x=>x.InstitutionId==tenant).Select(x=>x.Id).FirstAsync(),"DEMO counselling reference");
    }
    private async Task<Guid> Create(HttpClient c,AdmissionRequest? request=null)
    {
        var response=await c.PostAsJsonAsync(Path(),request??await Request());Assert.Equal(HttpStatusCode.Created,response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }
    [Fact] public async Task IndependentReviewKeepsHistoryWithoutCreatingEnrollment()
    {
        using var c=await fixture.Login("institution@example.test");using var reviewer=await fixture.Login("admin@example.test");
        using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();var before=await db.Enrollments.CountAsync();
        var id=await Create(c);var path=Path()+$"/{id}/transition";
        Assert.Equal(HttpStatusCode.OK,(await c.PostAsJsonAsync(path,new AdmissionTransition(1,"Submit","Applicant details checked"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync(path,new AdmissionTransition(2,"Review","Self review attempted","DEMO policy","DEMO eligibility","DEMO documents"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await reviewer.PostAsJsonAsync(path,new AdmissionTransition(2,"Review","Missing evidence"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await reviewer.PostAsJsonAsync(path,new AdmissionTransition(2,"Review","Evidence checked","DEMO policy version 1","DEMO evidence only","DEMO external document register"))).StatusCode);
        var detail=await c.GetFromJsonAsync<JsonElement>(Path()+$"/{id}");Assert.Equal("Reviewed",detail.GetProperty("application").GetProperty("status").GetString());Assert.Equal(3,detail.GetProperty("history").GetArrayLength());Assert.Equal(before,await db.Enrollments.CountAsync());
        Assert.Equal(3,await db.AuditLogs.CountAsync(x=>x.Resource==id.ToString()&&x.Action.StartsWith("Admission.")));
    }
    [Fact] public async Task DuplicateNumbersAreNormalizedAndConcurrentCreateIsAtomic()
    {
        using var c=await fixture.Login("institution@example.test");var request=await Request();
        var responses=await Task.WhenAll(c.PostAsJsonAsync(Path(),request with{ApplicationNumber=request.ApplicationNumber.ToLowerInvariant()}),c.PostAsJsonAsync(Path(),request));
        Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.Created);Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.Conflict);
        using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();var normalized=request.ApplicationNumber.ToUpperInvariant();Assert.Equal(1,await db.AdmissionApplications.CountAsync(x=>x.ApplicationNumber==normalized));
    }
    [Fact] public async Task ConcurrentSubmitAndEditAllowExactlyOneWriter()
    {
        using var c=await fixture.Login("institution@example.test");var request=await Request();var id=await Create(c,request);
        var responses=await Task.WhenAll(c.PostAsJsonAsync(Path()+$"/{id}/transition",new AdmissionTransition(1,"Submit","Submit current draft")),c.PutAsJsonAsync(Path()+$"/{id}",request with{FullName="Revised Applicant Demo"}));
        Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.OK);Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.Conflict);
        var detail=await c.GetFromJsonAsync<JsonElement>(Path()+$"/{id}");Assert.Equal(2,detail.GetProperty("history").GetArrayLength());Assert.Equal(2,detail.GetProperty("application").GetProperty("version").GetInt32());
    }
    [Fact] public async Task CancelRetainsRecordsAndBlocksReplayOrEdit()
    {
        using var c=await fixture.Login("institution@example.test");var request=await Request();var id=await Create(c,request);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Path()+$"/{id}/transition",new AdmissionTransition(1,"Cancel",""))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await c.PostAsJsonAsync(Path()+$"/{id}/transition",new AdmissionTransition(1,"Cancel","Applicant withdrew request"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PutAsJsonAsync(Path()+$"/{id}",request with{Version=2})).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(Path()+$"/{id}/transition",new AdmissionTransition(2,"Submit","Replay cancelled application"))).StatusCode);
        var detail=await c.GetFromJsonAsync<JsonElement>(Path()+$"/{id}");Assert.Equal("Cancelled",detail.GetProperty("application").GetProperty("status").GetString());Assert.Equal(2,detail.GetProperty("history").GetArrayLength());
    }
    [Fact] public async Task SubmittedDetailsAreLockedAndRejectRequiresAnotherAdministrator()
    {
        using var c=await fixture.Login("institution@example.test");using var reviewer=await fixture.Login("admin@example.test");var request=await Request();var id=await Create(c,request);var path=Path()+$"/{id}/transition";
        Assert.Equal(HttpStatusCode.OK,(await c.PostAsJsonAsync(path,new AdmissionTransition(1,"Submit","Ready for independent review"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PutAsJsonAsync(Path()+$"/{id}",request with{Version=2})).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync(path,new AdmissionTransition(2,"Reject","Self rejection attempted"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await reviewer.PostAsJsonAsync(path,new AdmissionTransition(2,"Reject","Evidence does not support application"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await reviewer.PostAsJsonAsync(path,new AdmissionTransition(2,"Review","Stale review","Policy","Eligibility","Documents"))).StatusCode);
    }
    [Fact] public async Task FacultyAndUnassignedInstitutionCannotReadOrWriteApplications()
    {
        using var c=await fixture.Login("institution@example.test");using var faculty=await fixture.Login("faculty@example.test");var request=await Request();var id=await Create(c,request);
        Assert.Equal(HttpStatusCode.Forbidden,(await faculty.GetAsync(Path())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await faculty.GetAsync(Path()+"/summary")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await faculty.PostAsJsonAsync(Path(),request)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync(Path(fixture.Vdc))).StatusCode);
        using var admin=await fixture.Login("admin@example.test");Assert.Equal(HttpStatusCode.NotFound,(await admin.GetAsync(Path(fixture.Vdc)+$"/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await admin.PostAsJsonAsync(Path(fixture.Vdc)+$"/{id}/transition",new AdmissionTransition(1,"Submit","Foreign application"))).StatusCode);
    }
    [Fact] public async Task ForeignReferencesAndMalformedInputsCreateNoHistory()
    {
        using var c=await fixture.Login("institution@example.test");using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();var before=await db.AdmissionEvents.CountAsync();
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Path(),await Request(fixture.Vdc))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Path(),(await Request()) with{Email="invalid",FullName=""})).StatusCode);
        Assert.Equal(before,await db.AdmissionEvents.CountAsync());
        Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync(Path()+"?status=Enrolled")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync(Path()+"?search="+new string('a',101))).StatusCode);
    }
    [Fact] public async Task SearchAndSummaryAreScopedAndDirectEnrollmentActionDoesNotExist()
    {
        using var c=await fixture.Login("institution@example.test");var request=await Request();var id=await Create(c,request);
        var list=await c.GetFromJsonAsync<JsonElement>(Path()+"?search="+request.ApplicationNumber+"&status=Draft");Assert.Equal(1,list.GetProperty("total").GetInt32());
        Assert.All(list.GetProperty("items").EnumerateArray(),x=>Assert.Equal(fixture.Vit,x.GetProperty("institutionId").GetGuid()));
        var counts=await c.GetFromJsonAsync<JsonElement>(Path()+"/summary");Assert.Contains(counts.EnumerateArray(),x=>x.GetProperty("status").GetString()=="Draft"&&x.GetProperty("count").GetInt32()>0);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(Path()+$"/{id}/transition",new AdmissionTransition(1,"Enroll","Try bypassing finance"))).StatusCode);
        c.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Path()+$"/{id}/transition",new AdmissionTransition(1,"Submit","Missing CSRF"))).StatusCode);
    }
}
