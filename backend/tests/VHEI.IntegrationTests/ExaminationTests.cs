using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VHEI.Api;
using VHEI.Application;
using VHEI.Domain;
using VHEI.Infrastructure;
using Xunit;
namespace VHEI.IntegrationTests;
public class ResultCalculationTests
{
    [Fact]public void WeightedGpaIncludesFailedCreditsAndUsesIndependentExpectedValues()
    {
        Assert.Equal(6.67m,ResultCalculator.Gpa([(3m,10m),(6m,5m)]));Assert.Equal(4m,ResultCalculator.Gpa([(3m,10m),(3m,0m),(3m,2m)]));
    }
    [Fact]public void ComponentFailureOverridesHighTotalAndAbsenceNeverPasses()
    {
        var e=new ExamAssessment{MaximumInternal=30,MaximumExternal=70,PassInternal=12,PassExternal=28,PassTotal=40};GradeBand[] bands=[new(0,"F",0),new(40,"P",5),new(80,"D",10)];var result=ResultCalculator.Calculate(e,new ExamMark{Attendance="Present",InternalMark=10,ExternalMark=70},bands,"F");Assert.Equal(80m,result.Total);Assert.False(result.IsPass);Assert.Equal("F",result.Grade);Assert.Equal(0m,result.GradePoints);Assert.False(ResultCalculator.Calculate(e,new ExamMark{Attendance="Absent"},bands,"F").IsPass);
    }
    [Fact]public void RoundedPercentageDoesNotOverrideActualTotalPassThreshold()
    {
        var e=new ExamAssessment{MaximumInternal=200,MaximumExternal=100,PassTotal=120};var result=ResultCalculator.Calculate(e,new ExamMark{Attendance="Present",InternalMark=49.99m,ExternalMark=70m},[new(0,"F",0),new(40,"P",5)],"F");Assert.Equal(40m,result.Percentage);Assert.False(result.IsPass);Assert.Equal("F",result.Grade);
    }
}
[CollectionDefinition("PostgresExams")]public class ExamCollection:ICollectionFixture<CoreFixture>{}
[Collection("PostgresExams")]
public class ExaminationTests(CoreFixture fixture)
{
    private string Root(Guid? institution=null)=>$"/api/v1/institutions/{institution??fixture.Vit}";
    private string Path(Guid id)=>Root()+$"/examinations/{id}";
    private async Task<(Guid Id,MarkInput[] Roster)> Setup(HttpClient author,HttpClient reviewer,string? section=null,decimal credits=3)
    {
        using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();var studentId=await db.PortalAccesses.Where(x=>x.InstitutionId==fixture.Vit&&x.Kind=="Student").Select(x=>x.StudentId).FirstAsync();var enrollment=await db.Enrollments.FirstAsync(x=>x.InstitutionId==fixture.Vit&&(section==null?x.StudentId==studentId:x.Section==section));var session=await db.AcademicSessions.SingleAsync(x=>x.Id==enrollment.AcademicSessionId);var teacher=await db.Faculty.Where(x=>x.InstitutionId==fixture.Vit&&x.UserId!=null).Select(x=>x.Id).FirstAsync();var suffix=Guid.NewGuid().ToString("N");
        var policy=(await(await author.PostAsJsonAsync(Root()+"/examinations/policies",new GradingPolicyRequest("DEMO grading "+suffix,enrollment.ProgrammeId,enrollment.AcademicSessionId,session.Name,"DEMO grading source; not official","F",[new(0,"F",0),new(40,"P",5),new(80,"D",10)]))).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();(await reviewer.PostAsJsonAsync(Root()+$"/examinations/policies/{policy}/approve",new ArchiveRequest(1,"DEMO policy checked"))).EnsureSuccessStatusCode();
        var subject=(await(await author.PostAsJsonAsync(Root()+"/academic/subjects",new SubjectRequest("EX-"+suffix,"DEMO exam subject "+suffix,enrollment.ProgrammeId,enrollment.AcademicSessionId,enrollment.Semester))).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var response=await author.PostAsJsonAsync(Root()+"/examinations",new AssessmentRequest("DEMO assessment "+suffix,subject,policy,teacher,enrollment.Section,DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1),"DEMO component/credits reference",credits,30,70,12,28,40));Assert.Equal(HttpStatusCode.Created,response.StatusCode);var id=(await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();var detail=await author.GetFromJsonAsync<JsonElement>(Path(id));return(id,detail.GetProperty("marks").EnumerateArray().Select(m=>new MarkInput(m.GetProperty("studentId").GetGuid(),"Present",30,50)).ToArray());
    }
    private async Task Submit(HttpClient c,(Guid Id,MarkInput[] Roster) e)
    {
        Assert.Equal(HttpStatusCode.OK,(await c.PutAsJsonAsync(Path(e.Id)+"/marks",new MarksRequest(1,e.Roster))).StatusCode);Assert.Equal(HttpStatusCode.OK,(await c.PostAsJsonAsync(Path(e.Id)+"/submit",new ArchiveRequest(2,"DEMO marks submitted"))).StatusCode);
    }
    [Fact]public async Task ResultsStayPrivateUntilIndependentApprovalAndPublication()
    {
        using var author=await fixture.Login("institution@example.test");using var reviewer=await fixture.Login("admin@example.test");using var student=await fixture.Login("student@example.test");var e=await Setup(author,reviewer);var studentId=(await student.GetFromJsonAsync<JsonElement>(Root()+"/portal/students"))[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Conflict,(await reviewer.PostAsJsonAsync(Path(e.Id)+"/publish",new ArchiveRequest(1,"Premature publish"))).StatusCode);await Submit(author,e);Assert.Equal(HttpStatusCode.Forbidden,(await author.PostAsJsonAsync(Path(e.Id)+"/approve",new ArchiveRequest(3,"Self approval"))).StatusCode);Assert.Equal(HttpStatusCode.OK,(await reviewer.PostAsJsonAsync(Path(e.Id)+"/approve",new ArchiveRequest(3,"DEMO independent result approval"))).StatusCode);
        var hidden=await student.GetFromJsonAsync<JsonElement>(Root()+$"/portal/students/{studentId}/results");Assert.DoesNotContain(hidden.GetProperty("results").EnumerateArray(),x=>x.GetProperty("assessmentId").GetGuid()==e.Id);Assert.Equal(HttpStatusCode.OK,(await reviewer.PostAsJsonAsync(Path(e.Id)+"/publish",new ArchiveRequest(4,"DEMO publication approved"))).StatusCode);
        var visible=await student.GetFromJsonAsync<JsonElement>(Root()+$"/portal/students/{studentId}/results");var result=Assert.Single(visible.GetProperty("results").EnumerateArray(),x=>x.GetProperty("assessmentId").GetGuid()==e.Id);Assert.Equal(80m,result.GetProperty("total").GetDecimal());Assert.Equal("D",result.GetProperty("grade").GetString());Assert.Equal(10m,result.GetProperty("gradePoints").GetDecimal());Assert.Equal(HttpStatusCode.Conflict,(await author.PutAsJsonAsync(Path(e.Id)+"/marks",new MarksRequest(5,e.Roster))).StatusCode);Assert.Equal(HttpStatusCode.Conflict,(await reviewer.PostAsJsonAsync(Path(e.Id)+"/publish",new ArchiveRequest(5,"Replay publish"))).StatusCode);
    }
    [Fact]public async Task PartialOutOfRangeAndAbsentMarksAreRejectedAtomically()
    {
        using var author=await fixture.Login("institution@example.test");using var reviewer=await fixture.Login("admin@example.test");var e=await Setup(author,reviewer);Assert.Equal(HttpStatusCode.BadRequest,(await author.PostAsJsonAsync(Path(e.Id)+"/submit",new ArchiveRequest(1,"Unentered marks"))).StatusCode);Assert.Equal(HttpStatusCode.BadRequest,(await author.PutAsJsonAsync(Path(e.Id)+"/marks",new MarksRequest(1,[e.Roster[0] with{StudentId=Guid.NewGuid()}]))).StatusCode);
        var tooHigh=e.Roster.Select((m,i)=>i==0?m with{ExternalMark=71}:m).ToArray();Assert.Equal(HttpStatusCode.BadRequest,(await author.PutAsJsonAsync(Path(e.Id)+"/marks",new MarksRequest(1,tooHigh))).StatusCode);var absent=e.Roster.Select((m,i)=>i==0?m with{Attendance="Absent"}:m).ToArray();Assert.Equal(HttpStatusCode.BadRequest,(await author.PutAsJsonAsync(Path(e.Id)+"/marks",new MarksRequest(1,absent))).StatusCode);
        var detail=await author.GetFromJsonAsync<JsonElement>(Path(e.Id));Assert.Equal(1,detail.GetProperty("assessment").GetProperty("version").GetInt32());Assert.All(detail.GetProperty("marks").EnumerateArray(),m=>Assert.Equal("Unentered",m.GetProperty("attendance").GetString()));
    }
    [Fact]public async Task ConcurrentMarksEditsAllowOneVersionOnly()
    {
        using var author=await fixture.Login("institution@example.test");using var reviewer=await fixture.Login("admin@example.test");var e=await Setup(author,reviewer);var r=new MarksRequest(1,e.Roster);var responses=await Task.WhenAll(author.PutAsJsonAsync(Path(e.Id)+"/marks",r),author.PutAsJsonAsync(Path(e.Id)+"/marks",r));Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.OK);Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.Conflict);
    }
    [Fact]public async Task FutureAssessmentCannotSubmitResultsEarly()
    {
        using var author=await fixture.Login("institution@example.test");using var reviewer=await fixture.Login("admin@example.test");var e=await Setup(author,reviewer);(await author.PutAsJsonAsync(Path(e.Id)+"/marks",new MarksRequest(1,e.Roster))).EnsureSuccessStatusCode();using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();var exam=await db.ExamAssessments.SingleAsync(x=>x.Id==e.Id);exam.ScheduledOn=DateOnly.FromDateTime(DateTime.UtcNow.AddMinutes(330)).AddDays(2);await db.SaveChangesAsync();var response=await author.PostAsJsonAsync(Path(e.Id)+"/submit",new ArchiveRequest(2,"Future marks submission"));Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);Assert.Contains("scheduled assessment date",await response.Content.ReadAsStringAsync());
    }
    [Fact]public async Task AssignedFacultyCanSubmitButCannotPublishAndOtherRolesCannotRead()
    {
        using var author=await fixture.Login("institution@example.test");using var reviewer=await fixture.Login("admin@example.test");using var faculty=await fixture.Login("faculty@example.test");using var student=await fixture.Login("student@example.test");var e=await Setup(author,reviewer);await Submit(faculty,e);Assert.Equal(HttpStatusCode.Forbidden,(await faculty.PostAsJsonAsync(Path(e.Id)+"/approve",new ArchiveRequest(3,"Faculty approval"))).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await student.GetAsync(Path(e.Id))).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await author.GetAsync(Root(fixture.Vdc)+"/examinations")).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await reviewer.GetAsync(Root(fixture.Vdc)+$"/examinations/{e.Id}")).StatusCode);
        using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();var other=await db.Students.Where(x=>x.InstitutionId==fixture.Vit&&!db.PortalAccesses.Any(a=>a.StudentId==x.Id)).Select(x=>x.Id).FirstAsync();Assert.Equal(HttpStatusCode.NotFound,(await student.GetAsync(Root()+$"/portal/students/{other}/results")).StatusCode);
    }
    [Fact]public async Task PublishedSemesterGpaMatchesIndependentWeightedExpectedValue()
    {
        using var author=await fixture.Login("institution@example.test");using var reviewer=await fixture.Login("admin@example.test");using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();var initial=await db.Enrollments.FirstAsync(x=>x.InstitutionId==fixture.Vit);var key=Guid.NewGuid().ToString("N");var section=("EX"+key[..8]).ToUpperInvariant();
        var s=(await(await author.PostAsJsonAsync(Root()+"/students",new StudentRequest("EX-"+key,"DEMO GPA Student","gpa-"+key+"@example.test","Active",initial.ProgrammeId,initial.AcademicSessionId,2026,initial.Semester,section,"DEMO Guardian","gpa-guardian@example.test"))).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var high=await Setup(author,reviewer,section,3);var low=await Setup(author,reviewer,section,6);await Submit(author,high);await Submit(author,(low.Id,low.Roster.Select(m=>m with{InternalMark=20,ExternalMark=30}).ToArray()));foreach(var e in new[]{high,low}){(await reviewer.PostAsJsonAsync(Path(e.Id)+"/approve",new ArchiveRequest(3,"DEMO weighted results checked"))).EnsureSuccessStatusCode();(await reviewer.PostAsJsonAsync(Path(e.Id)+"/publish",new ArchiveRequest(4,"DEMO results published"))).EnsureSuccessStatusCode();}
        var results=await reviewer.GetFromJsonAsync<JsonElement>(Root()+$"/students/{s}/results");Assert.Equal(6.67m,results.GetProperty("semesters")[0].GetProperty("sgpa").GetDecimal());Assert.Equal(6.67m,results.GetProperty("cumulative")[0].GetProperty("cgpa").GetDecimal());Assert.Equal(9m,results.GetProperty("cumulative")[0].GetProperty("credits").GetDecimal());
    }
}
