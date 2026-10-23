using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VHEI.Api;
using VHEI.Domain;
using VHEI.Infrastructure;
using Xunit;
namespace VHEI.IntegrationTests;
[CollectionDefinition("PostgresCertificates")]public class CertificateCollection:ICollectionFixture<CoreFixture>{}
[Collection("PostgresCertificates")]
public class CertificateTests(CoreFixture fixture)
{
    private string Root(Guid? institution=null)=>$"/api/v1/institutions/{institution??fixture.Vit}";
    private async Task<(Guid Student,Guid Subject,Guid Programme,Guid Session)> Published(HttpClient author,HttpClient reviewer,bool portal=false)
    {
        using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        var existing=await db.PortalAccesses.Where(x=>x.InstitutionId==fixture.Vit&&x.Kind=="Student").Select(x=>x.StudentId).FirstAsync();var enrollment=await db.Enrollments.FirstAsync(x=>x.StudentId==existing&&x.InstitutionId==fixture.Vit);var student=existing;var key=Guid.NewGuid().ToString("N");var section=("C"+key[..8]).ToUpperInvariant();
        if(!portal){var response=await author.PostAsJsonAsync(Root()+"/students",new StudentRequest("C-"+key,"DEMO Certificate Student","cert-"+key+"@example.test","Active",enrollment.ProgrammeId,enrollment.AcademicSessionId,2026,enrollment.Semester,section,"DEMO Guardian","certguardian@example.test"));response.EnsureSuccessStatusCode();student=(await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();}else section=enrollment.Section;
        var session=await db.AcademicSessions.SingleAsync(x=>x.Id==enrollment.AcademicSessionId);var teacher=await db.Faculty.Where(x=>x.InstitutionId==fixture.Vit&&x.UserId!=null).Select(x=>x.Id).FirstAsync();
        var p=await author.PostAsJsonAsync(Root()+"/examinations/policies",new GradingPolicyRequest("DEMO certificate grading "+key,enrollment.ProgrammeId,enrollment.AcademicSessionId,session.Name,"DEMO grading source","F",[new(0,"F",0),new(40,"P",5)]));p.EnsureSuccessStatusCode();var policy=(await p.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();(await reviewer.PostAsJsonAsync(Root()+$"/examinations/policies/{policy}/approve",new ArchiveRequest(1,"DEMO independent grading approval"))).EnsureSuccessStatusCode();
        var s=await author.PostAsJsonAsync(Root()+"/academic/subjects",new SubjectRequest("CERT-"+key,"DEMO certificate subject "+key,enrollment.ProgrammeId,enrollment.AcademicSessionId,enrollment.Semester));s.EnsureSuccessStatusCode();var subject=(await s.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var e=await author.PostAsJsonAsync(Root()+"/examinations",new AssessmentRequest("DEMO certificate assessment "+key,subject,policy,teacher,section,DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1),"DEMO component source",3,30,70,12,28,40));e.EnsureSuccessStatusCode();var exam=(await e.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();var path=Root()+$"/examinations/{exam}";var sheet=await author.GetFromJsonAsync<JsonElement>(path);var roster=sheet.GetProperty("marks").EnumerateArray().Select(m=>new MarkInput(m.GetProperty("studentId").GetGuid(),"Present",30,50)).ToArray();
        (await author.PutAsJsonAsync(path+"/marks",new MarksRequest(1,roster))).EnsureSuccessStatusCode();(await author.PostAsJsonAsync(path+"/submit",new ArchiveRequest(2,"DEMO submitted marks"))).EnsureSuccessStatusCode();(await reviewer.PostAsJsonAsync(path+"/approve",new ArchiveRequest(3,"DEMO independent approval"))).EnsureSuccessStatusCode();(await reviewer.PostAsJsonAsync(path+"/publish",new ArchiveRequest(4,"DEMO publication"))).EnsureSuccessStatusCode();return(student,subject,enrollment.ProgrammeId,enrollment.AcademicSessionId);
    }
    private async Task<Guid> Request(HttpClient client,Guid student,string kind="Transcript",Guid? policy=null,string clearance="")
    {
        var r=await client.PostAsJsonAsync(Root()+$"/certificates/students/{student}",new CertificateRequest(kind,policy,clearance));Assert.Equal(HttpStatusCode.Created,r.StatusCode);return(await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }
    private Task<HttpResponseMessage> Issue(HttpClient c,Guid id,int version=1)=>c.PostAsJsonAsync(Root()+$"/certificates/{id}/issue",new CertificateIssueRequest(version,"DEMO record independently reviewed","DEMO authority reference; no official signature"));
    [Fact]public async Task PortalRequestIndependentIssueImmutablePdfAndPublicRevocation()
    {
        using var a=await fixture.Login("institution@example.test");using var r=await fixture.Login("admin@example.test");using var student=await fixture.Login("student@example.test");var data=await Published(a,r,true);var portal=Root()+$"/portal/students/{data.Student}/certificates";
        var response=await student.PostAsync(portal,null);Assert.Equal(HttpStatusCode.Created,response.StatusCode);var id=(await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();Assert.Equal(HttpStatusCode.Conflict,(await student.PostAsync(portal,null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await student.GetAsync(portal+$"/{id}/download")).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await Issue(student,id)).StatusCode);Assert.Equal(HttpStatusCode.OK,(await Issue(r,id)).StatusCode);
        var file=await student.GetAsync(portal+$"/{id}/download");Assert.Equal("application/pdf",file.Content.Headers.ContentType!.MediaType);Assert.Contains("no-store",file.Headers.CacheControl!.ToString());var bytes=await file.Content.ReadAsByteArrayAsync();Assert.StartsWith("%PDF-",System.Text.Encoding.ASCII.GetString(bytes,0,8));Assert.Equal(bytes,await(await student.GetAsync(portal+$"/{id}/download")).Content.ReadAsByteArrayAsync());
        using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();var saved=await db.AcademicCertificates.AsNoTracking().SingleAsync(x=>x.Id==id);Assert.Equal(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),saved.Sha256);
        using var anonymous=fixture.App.CreateClient();var verify=await anonymous.GetFromJsonAsync<JsonElement>("/api/v1/certificates/verify/"+saved.VerificationToken);Assert.Equal("Issued",verify.GetProperty("status").GetString());Assert.False(verify.TryGetProperty("student",out _));Assert.False(verify.TryGetProperty("snapshotJson",out _));Assert.Equal(HttpStatusCode.Conflict,(await Issue(r,id,2)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await r.PostAsJsonAsync(Root()+$"/certificates/{id}/revoke",new ArchiveRequest(2,"DEMO document withdrawn"))).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await student.GetAsync(portal+$"/{id}/download")).StatusCode);Assert.Equal("Revoked",(await anonymous.GetFromJsonAsync<JsonElement>("/api/v1/certificates/verify/"+saved.VerificationToken)).GetProperty("status").GetString());
    }
    [Fact]public async Task SelfIssuanceForeignScopeAndUnlinkedPortalAreRejected()
    {
        using var a=await fixture.Login("institution@example.test");using var r=await fixture.Login("admin@example.test");using var student=await fixture.Login("student@example.test");var data=await Published(a,r);var id=await Request(a,data.Student);
        Assert.Equal(HttpStatusCode.Forbidden,(await Issue(a,id)).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await student.GetAsync(Root()+$"/portal/students/{data.Student}/certificates")).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await a.GetAsync(Root(fixture.Vdc)+$"/certificates/students/{data.Student}")).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await r.GetAsync(Root(fixture.Vdc)+$"/certificates/{id}/download")).StatusCode);
        a.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");Assert.Equal(HttpStatusCode.BadRequest,(await a.PostAsJsonAsync(Root()+$"/certificates/{id}/cancel",new ArchiveRequest(1,"CSRF missing"))).StatusCode);
    }
    [Fact]public async Task ConcurrentIssuanceCreatesOneDocumentOnly()
    {
        using var a=await fixture.Login("institution@example.test");using var r=await fixture.Login("admin@example.test");var data=await Published(a,r);var id=await Request(a,data.Student);var responses=await Task.WhenAll(Issue(r,id),Issue(r,id));Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.OK);Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.Conflict);
    }
    [Fact]public async Task ChangedSnapshotCannotIssueAndCancelledRequestCanBeReplaced()
    {
        using var a=await fixture.Login("institution@example.test");using var r=await fixture.Login("admin@example.test");var data=await Published(a,r);var id=await Request(a,data.Student);
        using(var scope=fixture.App.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();var s=await db.Students.SingleAsync(x=>x.Id==data.Student);s.FullName="DEMO Corrected Certificate Name";s.Version++;await db.SaveChangesAsync();}
        Assert.Equal(HttpStatusCode.Conflict,(await Issue(r,id)).StatusCode);Assert.Equal(HttpStatusCode.OK,(await r.PostAsJsonAsync(Root()+$"/certificates/{id}/cancel",new ArchiveRequest(1,"Replaced after reviewed profile correction"))).StatusCode);var replacement=await Request(a,data.Student);Assert.Equal(HttpStatusCode.OK,(await Issue(r,replacement)).StatusCode);
    }
    [Fact]public async Task GraduationRequiresExplicitApprovedCurriculumCreditsAndClearance()
    {
        using var a=await fixture.Login("institution@example.test");using var r=await fixture.Login("admin@example.test");var data=await Published(a,r);
        async Task<Guid> Policy(decimal credits){var response=await a.PostAsJsonAsync(Root()+"/certificates/graduation-policies",new GraduationPolicyRequest("DEMO explicit curriculum",data.Programme,data.Session,[data.Subject],credits,"DEMO approved curriculum source, no official rules"));Assert.Equal(HttpStatusCode.Created,response.StatusCode);return(await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();}
        var policy=await Policy(4);var path=Root()+$"/certificates/graduation-policies/{policy}/approve";Assert.Equal(HttpStatusCode.Forbidden,(await a.PostAsJsonAsync(path,new ArchiveRequest(1,"Self approval"))).StatusCode);Assert.False((await r.GetFromJsonAsync<JsonElement>(Root()+$"/certificates/students/{data.Student}/eligibility/{policy}")).GetProperty("eligible").GetBoolean());
        (await r.PostAsJsonAsync(path,new ArchiveRequest(1,"DEMO independently checked curriculum"))).EnsureSuccessStatusCode();Assert.Equal(HttpStatusCode.Conflict,(await a.PostAsJsonAsync(Root()+$"/certificates/students/{data.Student}",new CertificateRequest("CourseCompletion",policy,"DEMO all clearances verified"))).StatusCode);
        var passing=await Policy(3);(await r.PostAsJsonAsync(Root()+$"/certificates/graduation-policies/{passing}/approve",new ArchiveRequest(1,"DEMO independently checked full curriculum"))).EnsureSuccessStatusCode();Assert.True((await r.GetFromJsonAsync<JsonElement>(Root()+$"/certificates/students/{data.Student}/eligibility/{passing}")).GetProperty("eligible").GetBoolean());
        Assert.Equal(HttpStatusCode.BadRequest,(await a.PostAsJsonAsync(Root()+$"/certificates/students/{data.Student}",new CertificateRequest("CourseCompletion",passing,""))).StatusCode);var id=await Request(a,data.Student,"CourseCompletion",passing,"DEMO finance/library/administration clearance evidence");Assert.Equal(HttpStatusCode.OK,(await Issue(r,id)).StatusCode);
    }
    [Fact]public async Task MissingSubjectAndWrongInstitutionCurriculaCannotConferGraduation()
    {
        using var a=await fixture.Login("institution@example.test");using var r=await fixture.Login("admin@example.test");var data=await Published(a,r);var foreign=await a.PostAsJsonAsync(Root()+"/certificates/graduation-policies",new GraduationPolicyRequest("Invalid curriculum",data.Programme,data.Session,[Guid.NewGuid()],1,"DEMO missing subject"));Assert.Equal(HttpStatusCode.BadRequest,foreign.StatusCode);
        using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();var missing=new Subject{InstitutionId=fixture.Vit,ProgrammeId=data.Programme,AcademicSessionId=data.Session,Semester=2,Code="MISS-"+Guid.NewGuid(),Name="DEMO unpublished required subject"};db.Subjects.Add(missing);await db.SaveChangesAsync();
        var response=await a.PostAsJsonAsync(Root()+"/certificates/graduation-policies",new GraduationPolicyRequest("DEMO incomplete curriculum",data.Programme,data.Session,[data.Subject,missing.Id],3,"DEMO required subject list"));var id=(await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();(await r.PostAsJsonAsync(Root()+$"/certificates/graduation-policies/{id}/approve",new ArchiveRequest(1,"DEMO approved full subject list"))).EnsureSuccessStatusCode();var eligibility=await r.GetFromJsonAsync<JsonElement>(Root()+$"/certificates/students/{data.Student}/eligibility/{id}");Assert.False(eligibility.GetProperty("eligible").GetBoolean());Assert.Equal(1,eligibility.GetProperty("passedSubjects").GetInt32());Assert.Equal(2,eligibility.GetProperty("requiredSubjects").GetInt32());
    }
    [Fact]public void PdfPaginatesLongNamesAndPreservesDemoLabelling()
    {
        var rows=Enumerable.Range(1,18).Select(i=>new{Code="LONG-"+i,subject=new string('X',120)+" DEMO subject",programme="DEMO Programme",session="2026-27",Semester=i,Credits=3,Total=80,Percentage=80,Grade="P",GradePoints=5,IsPass=true}).ToArray();
        var c=new AcademicCertificate{IsDemo=true,Kind="Transcript",Number="DEMO-LAYOUT-VALIDATION",IssuedAt=DateTime.UtcNow,AuthorityReference=new string('A',500),SnapshotJson=JsonSerializer.Serialize(new{institution="DEMO Institution for Academic Document Layout Validation",student=new string('N',120),studentNumber="DEMO-123",results=new{results=rows,semesters=Array.Empty<object>(),cumulative=Array.Empty<object>(),note="GPA covers recorded published assessments only."}})};
        var bytes=CertificatePdf.Render(c,"http://127.0.0.1:5173/api/v1/certificates/verify/"+new string('a',48));Assert.True(bytes.Length>10000);var output=Environment.GetEnvironmentVariable("CERTIFICATE_QA_OUTPUT");if(!string.IsNullOrWhiteSpace(output))File.WriteAllBytes(output,bytes);
        c.SnapshotJson=c.SnapshotJson.Replace(new string('N',120),"ଓଡ଼ିଆ");Assert.Throws<NotSupportedException>(()=>CertificatePdf.Render(c,"https://example.test/verify"));
    }
}
