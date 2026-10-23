using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using VHEI.Api;
using VHEI.Infrastructure;
using Xunit;
namespace VHEI.IntegrationTests;
[CollectionDefinition("PostgresFinance")] public class FinanceCollection:ICollectionFixture<CoreFixture>{}
[Collection("PostgresFinance")]
public class AdmissionFinanceTests(CoreFixture fixture)
{
    private string Root(Guid? tenant=null)=>$"/api/v1/institutions/{tenant??fixture.Vit}";
    private string App(Guid id)=>Root()+$"/admissions/{id}";
    private async Task<FeePlanRequest> PlanRequest(decimal amount=100m)
    {
        using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();var session=await db.AcademicSessions.FirstAsync(x=>x.InstitutionId==fixture.Vit);
        return new("DEMO fee plan "+Guid.NewGuid(),await db.Programmes.Where(x=>x.InstitutionId==fixture.Vit).Select(x=>x.Id).FirstAsync(),session.Id,session.Name,"DEMO synthetic fee reference; not official","DEMO admission authority only",amount);
    }
    private async Task<Guid> Plan(HttpClient author,HttpClient reviewer,FeePlanRequest r)
    {
        var response=await author.PostAsJsonAsync(Root()+"/finance/fee-plans",r);Assert.Equal(HttpStatusCode.Created,response.StatusCode);var id=(await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK,(await reviewer.PostAsJsonAsync(Root()+$"/finance/fee-plans/{id}/approve",new ArchiveRequest(1,"DEMO independently checked"))).StatusCode);return id;
    }
    private async Task<Guid> Reviewed(HttpClient author,HttpClient reviewer,FeePlanRequest r)
    {
        var response=await author.PostAsJsonAsync(Root()+"/admissions",new AdmissionRequest("APP-"+Guid.NewGuid().ToString("N"),"DEMO finance applicant","finance-applicant@example.test",r.ProgrammeId,r.AcademicSessionId,""));Assert.Equal(HttpStatusCode.Created,response.StatusCode);var id=(await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await author.PostAsJsonAsync(App(id)+"/transition",new AdmissionTransition(1,"Submit","DEMO submission"))).EnsureSuccessStatusCode();(await reviewer.PostAsJsonAsync(App(id)+"/transition",new AdmissionTransition(2,"Review","DEMO evidence checked","DEMO policy","DEMO eligibility","DEMO document register"))).EnsureSuccessStatusCode();return id;
    }
    private async Task<Guid> Charged(HttpClient author,HttpClient reviewer,decimal amount=100m)
    {
        var r=await PlanRequest(amount);var plan=await Plan(author,reviewer,r);var id=await Reviewed(author,reviewer,r);Assert.Equal(HttpStatusCode.Created,(await author.PostAsJsonAsync(App(id)+"/charge",new ChargeRequest(3,plan))).StatusCode);return id;
    }
    private static OfflinePaymentRequest Payment(decimal amount=100m)=>new(Guid.NewGuid(),"DEMO-"+Guid.NewGuid().ToString("N"),"DEMO evidence only","BankTransfer",DateOnly.FromDateTime(DateTime.UtcNow),amount);
    private async Task<Guid> Record(HttpClient c,Guid app,OfflinePaymentRequest r)
    {
        var response=await c.PostAsJsonAsync(App(app)+"/payments",r);Assert.Equal(HttpStatusCode.Created,response.StatusCode);return(await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }
    private async Task Confirm(HttpClient c,Guid payment)=>Assert.Equal(HttpStatusCode.OK,(await c.PostAsJsonAsync(Root()+$"/finance/payments/{payment}/review",new ReviewRequest(true,"DEMO evidence independently checked"))).StatusCode);
    private async Task<int> Version(HttpClient c,Guid id)=>(await c.GetFromJsonAsync<JsonElement>(App(id))).GetProperty("application").GetProperty("version").GetInt32();
    [Fact] public async Task FeePlansRequireIndependentSameYearApprovalAndPreciseAmounts()
    {
        using var author=await fixture.Login("institution@example.test");using var reviewer=await fixture.Login("admin@example.test");var r=await PlanRequest();
        Assert.Equal(HttpStatusCode.BadRequest,(await author.PostAsJsonAsync(Root()+"/finance/fee-plans",r with{Amount=10.001m})).StatusCode);Assert.Equal(HttpStatusCode.BadRequest,(await author.PostAsJsonAsync(Root()+"/finance/fee-plans",r with{Amount=-1m})).StatusCode);
        var draft=await(await author.PostAsJsonAsync(Root()+"/finance/fee-plans",r with{SourceAcademicYear="2025-26"})).Content.ReadFromJsonAsync<JsonElement>();var id=draft.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Forbidden,(await author.PostAsJsonAsync(Root()+$"/finance/fee-plans/{id}/approve",new ArchiveRequest(1,"Self approval"))).StatusCode);Assert.Equal(HttpStatusCode.Conflict,(await reviewer.PostAsJsonAsync(Root()+$"/finance/fee-plans/{id}/approve",new ArchiveRequest(1,"Wrong source year"))).StatusCode);
        var app=await Reviewed(author,reviewer,r);Assert.Equal(HttpStatusCode.BadRequest,(await author.PostAsJsonAsync(App(app)+"/charge",new ChargeRequest(3,id))).StatusCode);
        var ledger=await author.GetFromJsonAsync<JsonElement>(App(app)+"/finance");Assert.Equal(JsonValueKind.Null,ledger.GetProperty("charge").ValueKind);Assert.Equal(0,ledger.GetProperty("payments").GetArrayLength());
    }
    [Fact] public async Task PaymentConfirmationAndFinalApprovalGateAtomicEnrollment()
    {
        using var author=await fixture.Login("institution@example.test");using var reviewer=await fixture.Login("admin@example.test");var app=await Charged(author,reviewer);
        Assert.Equal(HttpStatusCode.Conflict,(await reviewer.PostAsJsonAsync(App(app)+"/approve",new FinalAdmissionRequest(4,"DEMO approval","No confirmed money"))).StatusCode);Assert.Equal(HttpStatusCode.Conflict,(await author.PostAsJsonAsync(App(app)+"/enroll",new AdmissionEnrollmentRequest(4,"NO-GATE","Guardian Demo","guardian@example.test",2026,1,"A"))).StatusCode);
        var p=await Record(author,app,Payment());Assert.Equal(HttpStatusCode.Forbidden,(await author.PostAsJsonAsync(Root()+$"/finance/payments/{p}/review",new ReviewRequest(true,"Self check"))).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await reviewer.GetAsync(Root()+$"/finance/payments/{p}/acknowledgement")).StatusCode);await Confirm(reviewer,p);
        Assert.Equal(HttpStatusCode.Forbidden,(await author.PostAsJsonAsync(App(app)+"/approve",new FinalAdmissionRequest(4,"DEMO authority","Self final approval"))).StatusCode);Assert.Equal(HttpStatusCode.OK,(await reviewer.PostAsJsonAsync(App(app)+"/approve",new FinalAdmissionRequest(4,"DEMO authority","Final evidence and fees checked"))).StatusCode);
        var number="FIN-"+Guid.NewGuid().ToString("N");var r=new AdmissionEnrollmentRequest(5,number,"DEMO Guardian","guardian@example.test",2026,1,"A");var responses=await Task.WhenAll(author.PostAsJsonAsync(App(app)+"/enroll",r),author.PostAsJsonAsync(App(app)+"/enroll",r));Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.Created);Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.Conflict);
        var detail=await author.GetFromJsonAsync<JsonElement>(App(app));Assert.Equal("Enrolled",detail.GetProperty("application").GetProperty("status").GetString());var studentId=detail.GetProperty("application").GetProperty("studentId").GetGuid();using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();Assert.Equal(1,await db.Enrollments.CountAsync(x=>x.StudentId==studentId));Assert.Equal(1,await db.Students.CountAsync(x=>x.StudentNumber==number.ToUpperInvariant()));Assert.True(await db.AuditLogs.AnyAsync(x=>x.Action=="Student.CreatedFromAdmission"&&x.Resource==studentId.ToString()));
        var ack=await author.GetStringAsync(Root()+$"/finance/payments/{p}/acknowledgement");Assert.Contains("DEMO",ack);Assert.Contains("INR 100.00",ack);Assert.Contains("ACK-",ack);
    }
    [Fact] public async Task IdempotencyPreventsDuplicatesAndOverpaymentAcrossConcurrentRequests()
    {
        using var author=await fixture.Login("institution@example.test");using var reviewer=await fixture.Login("admin@example.test");var app=await Charged(author,reviewer);var r=Payment(60m);
        var responses=await Task.WhenAll(author.PostAsJsonAsync(App(app)+"/payments",r),author.PostAsJsonAsync(App(app)+"/payments",r));Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.Created);Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.OK);
        Assert.Equal(HttpStatusCode.Conflict,(await author.PostAsJsonAsync(App(app)+"/payments",r with{Amount=40m})).StatusCode);Assert.Equal(HttpStatusCode.Conflict,(await author.PostAsJsonAsync(App(app)+"/payments",Payment(50m))).StatusCode);
        var more=await Task.WhenAll(author.PostAsJsonAsync(App(app)+"/payments",Payment(40m)),author.PostAsJsonAsync(App(app)+"/payments",Payment(40m)));Assert.Single(more,x=>x.StatusCode==HttpStatusCode.Created);Assert.Single(more,x=>x.StatusCode==HttpStatusCode.Conflict);
        var ledger=await author.GetFromJsonAsync<JsonElement>(App(app)+"/finance");Assert.Equal(2,ledger.GetProperty("payments").GetArrayLength());Assert.Equal(0m,ledger.GetProperty("confirmed").GetDecimal());
    }
    [Fact] public async Task RejectedPaymentReleasesReservationAndCannotBeReviewedAgain()
    {
        using var author=await fixture.Login("institution@example.test");using var reviewer=await fixture.Login("admin@example.test");var app=await Charged(author,reviewer);var id=await Record(author,app,Payment());
        Assert.Equal(HttpStatusCode.Conflict,(await author.PostAsJsonAsync(App(app)+"/transition",new AdmissionTransition(4,"Cancel","Pending payment cancellation"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await reviewer.PostAsJsonAsync(Root()+$"/finance/payments/{id}/review",new ReviewRequest(false,"DEMO evidence rejected"))).StatusCode);Assert.Equal(HttpStatusCode.Conflict,(await reviewer.PostAsJsonAsync(Root()+$"/finance/payments/{id}/review",new ReviewRequest(true,"Review replay"))).StatusCode);
        var p=await Record(author,app,Payment());await Confirm(reviewer,p);Assert.Equal(HttpStatusCode.Conflict,(await author.PostAsJsonAsync(App(app)+"/transition",new AdmissionTransition(4,"Cancel","Paid record cancellation"))).StatusCode);
    }
    [Fact] public async Task InvalidAndDuplicateReferencesLeaveBalancesAndAuditsUnchanged()
    {
        using var author=await fixture.Login("institution@example.test");using var reviewer=await fixture.Login("admin@example.test");var app=await Charged(author,reviewer);var r=Payment(10m);await Record(author,app,r);
        Assert.Equal(HttpStatusCode.Conflict,(await author.PostAsJsonAsync(App(app)+"/payments",r with{IdempotencyKey=Guid.NewGuid()})).StatusCode);
        foreach(var invalid in new[]{Payment(0m),Payment(0.001m),Payment() with{PaidOn=DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2)},Payment() with{ExternalReference="DEMO\nINJECT"}})Assert.Equal(HttpStatusCode.BadRequest,(await author.PostAsJsonAsync(App(app)+"/payments",invalid)).StatusCode);
        var ledger=await author.GetFromJsonAsync<JsonElement>(App(app)+"/finance");Assert.Equal(1,ledger.GetProperty("payments").GetArrayLength());
    }
    [Fact] public async Task DuplicateStudentNumberRollsBackGuardianAndConversion()
    {
        using var author=await fixture.Login("institution@example.test");using var reviewer=await fixture.Login("admin@example.test");var app=await Charged(author,reviewer);await Confirm(reviewer,await Record(author,app,Payment()));(await reviewer.PostAsJsonAsync(App(app)+"/approve",new FinalAdmissionRequest(4,"DEMO authority","Verified demo approval"))).EnsureSuccessStatusCode();
        using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();var existing=await db.Students.Where(x=>x.InstitutionId==fixture.Vit).Select(x=>x.StudentNumber).FirstAsync();var guardians=await db.Guardians.CountAsync();
        Assert.Equal(HttpStatusCode.Conflict,(await author.PostAsJsonAsync(App(app)+"/enroll",new AdmissionEnrollmentRequest(5,existing,"DEMO guardian","guardian@example.test",2026,1,"A"))).StatusCode);Assert.Equal(guardians,await db.Guardians.CountAsync());Assert.Equal(5,await Version(author,app));
    }
    [Fact] public async Task ForeignProgrammeChargeAndPrivateFinanceAccessAreDenied()
    {
        using var author=await fixture.Login("institution@example.test");using var reviewer=await fixture.Login("admin@example.test");using var faculty=await fixture.Login("faculty@example.test");var r=await PlanRequest();var app=await Reviewed(author,reviewer,r);
        Assert.Equal(HttpStatusCode.Forbidden,(await faculty.GetAsync(Root()+"/finance/fee-plans")).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await faculty.GetAsync(App(app)+"/finance")).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await faculty.PostAsJsonAsync(Root()+"/finance/fee-plans",r)).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await author.GetAsync(Root(fixture.Vdc)+"/finance/charges")).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await reviewer.GetAsync(Root(fixture.Vdc)+$"/admissions/{app}/finance")).StatusCode);
        using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();var foreign=await db.Programmes.Where(x=>x.InstitutionId==fixture.Vdc).Select(x=>x.Id).FirstAsync();Assert.Equal(HttpStatusCode.BadRequest,(await author.PostAsJsonAsync(Root()+"/finance/fee-plans",r with{ProgrammeId=foreign})).StatusCode);
        var other=await db.Programmes.Where(x=>x.InstitutionId==fixture.Vit&&x.Id!=r.ProgrammeId).Select(x=>x.Id).FirstAsync();var plan=await Plan(author,reviewer,r with{ProgrammeId=other});Assert.Equal(HttpStatusCode.BadRequest,(await author.PostAsJsonAsync(App(app)+"/charge",new ChargeRequest(3,plan))).StatusCode);
        author.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");Assert.Equal(HttpStatusCode.BadRequest,(await author.PostAsJsonAsync(App(app)+"/charge",new ChargeRequest(3,plan))).StatusCode);
    }
    [Fact] public async Task UnpaidCancellationVoidsChargeAndPreventsLaterPayment()
    {
        using var author=await fixture.Login("institution@example.test");using var reviewer=await fixture.Login("admin@example.test");var app=await Charged(author,reviewer);Assert.Equal(HttpStatusCode.OK,(await author.PostAsJsonAsync(App(app)+"/transition",new AdmissionTransition(4,"Cancel","Unpaid fictional application withdrawn"))).StatusCode);Assert.Equal(HttpStatusCode.Conflict,(await author.PostAsJsonAsync(App(app)+"/payments",Payment())).StatusCode);var ledger=await author.GetFromJsonAsync<JsonElement>(App(app)+"/finance");Assert.True(ledger.GetProperty("charge").GetProperty("isVoid").GetBoolean());
        var rows=await author.GetFromJsonAsync<JsonElement>(Root()+"/finance/charges");Assert.All(rows.GetProperty("items").EnumerateArray(),x=>Assert.True(x.GetProperty("amount").GetDecimal()>0));
    }
    [Fact] public async Task VdcUgRequiresExternalPortalPaymentAndSelectionEvidence()
    {
        using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();var users=scope.ServiceProvider.GetRequiredService<UserManager<ErpUser>>();var email="vdc-finance-"+Guid.NewGuid().ToString("N")+"@example.test";var user=new ErpUser{UserName=email,Email=email,DisplayName="DEMO VDC finance reviewer",EmailConfirmed=true};Assert.True((await users.CreateAsync(user,CoreFixture.Password)).Succeeded);Assert.True((await users.AddToRoleAsync(user,"InstitutionAdministrator")).Succeeded);db.InstitutionAssignments.Add(new VHEI.Domain.InstitutionAssignment{UserId=user.Id,InstitutionId=fixture.Vdc});await db.SaveChangesAsync();
        using var author=await fixture.Login("admin@example.test");using var reviewer=await fixture.Login(email);var root=Root(fixture.Vdc);var programme=await db.Programmes.FirstAsync(x=>x.InstitutionId==fixture.Vdc&&x.Name.StartsWith("+3"));var session=await db.AcademicSessions.FirstAsync(x=>x.InstitutionId==fixture.Vdc);var planRequest=new FeePlanRequest("DEMO VDC policy test",programme.Id,session.Id,session.Name,"DEMO fee reference","DEMO authority",100m);
        var draft=await(await author.PostAsJsonAsync(root+"/finance/fee-plans",planRequest)).Content.ReadFromJsonAsync<JsonElement>();Assert.Equal(HttpStatusCode.Conflict,(await reviewer.PostAsJsonAsync(root+$"/finance/fee-plans/{draft.GetProperty("id").GetGuid()}/approve",new ArchiveRequest(1,"Offline plan not allowed"))).StatusCode);
        var portal=await(await author.PostAsJsonAsync(root+"/finance/fee-plans",planRequest with{CollectionMode="ExternalPortal"})).Content.ReadFromJsonAsync<JsonElement>();(await reviewer.PostAsJsonAsync(root+$"/finance/fee-plans/{portal.GetProperty("id").GetGuid()}/approve",new ArchiveRequest(1,"DEMO SAMS evidence mode"))).EnsureSuccessStatusCode();
        var a=await(await author.PostAsJsonAsync(root+"/admissions",new AdmissionRequest("SAMS-"+Guid.NewGuid().ToString("N"),"DEMO SAMS applicant","sams@example.test",programme.Id,session.Id,""))).Content.ReadFromJsonAsync<JsonElement>();var path=root+$"/admissions/{a.GetProperty("id").GetGuid()}";(await author.PostAsJsonAsync(path+"/transition",new AdmissionTransition(1,"Submit","DEMO submitted"))).EnsureSuccessStatusCode();(await reviewer.PostAsJsonAsync(path+"/transition",new AdmissionTransition(2,"Review","DEMO checked","DEMO policy","DEMO eligibility","DEMO docs"))).EnsureSuccessStatusCode();(await author.PostAsJsonAsync(path+"/charge",new ChargeRequest(3,portal.GetProperty("id").GetGuid()))).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.BadRequest,(await author.PostAsJsonAsync(path+"/payments",Payment())).StatusCode);var payment=await(await author.PostAsJsonAsync(path+"/payments",Payment() with{Method="ExternalPortal"})).Content.ReadFromJsonAsync<JsonElement>();(await reviewer.PostAsJsonAsync(root+$"/finance/payments/{payment.GetProperty("id").GetGuid()}/review",new ReviewRequest(true,"DEMO external portal evidence checked"))).EnsureSuccessStatusCode();Assert.Equal(HttpStatusCode.Conflict,(await reviewer.PostAsJsonAsync(path+"/approve",new FinalAdmissionRequest(4,"DEMO authority","Missing SAMS selection"))).StatusCode);
    }
}
