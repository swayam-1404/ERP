using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VHEI.Infrastructure;
using Xunit;
namespace VHEI.IntegrationTests;
[Collection("PostgresCore")]
public class AttendanceTests(CoreFixture fixture)
{
    private static int sequence;
    private async Task<(Guid Subject,Guid Room,Guid Teacher,string Section)> Setup()
    {
        using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();var subject=await db.Subjects.FirstAsync(x=>x.InstitutionId==fixture.Vit);var room=await db.Classrooms.FirstAsync(x=>x.InstitutionId==fixture.Vit);var teacher=await db.Faculty.FirstAsync(x=>x.InstitutionId==fixture.Vit&&x.UserId!=null);var section=await db.Enrollments.Where(x=>x.InstitutionId==fixture.Vit&&x.ProgrammeId==subject.ProgrammeId&&x.Semester==subject.Semester).Select(x=>x.Section).FirstAsync();return(subject.Id,room.Id,teacher.Id,section);
    }
    private async Task<Guid> Create(HttpClient client,DateTimeOffset? time=null)
    {
        var data=await Setup();var start=time??DateTimeOffset.UtcNow.Date.AddDays(-10).AddHours(Interlocked.Increment(ref sequence)*2);
        var response=await client.PostAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/academic/classes",new{subjectId=data.Subject,facultyId=data.Teacher,classroomId=data.Room,section=data.Section,startsAt=start,endsAt=start.AddHours(1)});response.EnsureSuccessStatusCode();return(await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }
    private string Path(Guid id)=>$"/api/v1/institutions/{fixture.Vit}/academic/classes/{id}/attendance";
    [Fact]public async Task OverlapRejectedAndAdjacentClassAllowed()
    {
        using var c=await fixture.Login("admin@example.test");var start=new DateTimeOffset(2026,8,1,6,0,0,TimeSpan.Zero);await Create(c,start);var data=await Setup();var r=await c.PostAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/academic/classes",new{subjectId=data.Subject,facultyId=data.Teacher,classroomId=data.Room,section=data.Section,startsAt=start.AddMinutes(30),endsAt=start.AddMinutes(90)});Assert.Equal(HttpStatusCode.Conflict,r.StatusCode);await Create(c,start.AddHours(1));
    }
    [Fact]public async Task SimultaneousBookingAllowsOneClassOnly()
    {
        using var c=await fixture.Login("admin@example.test");var d=await Setup();var start=new DateTimeOffset(2026,8,2,6,0,0,TimeSpan.Zero);var r=new{subjectId=d.Subject,facultyId=d.Teacher,classroomId=d.Room,section=d.Section,startsAt=start,endsAt=start.AddHours(1)};var responses=await Task.WhenAll(c.PostAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/academic/classes",r),c.PostAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/academic/classes",r));Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.Created);Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.Conflict);
    }
    [Fact]public async Task AssignedFacultySubmitsCompleteRosterOnceAndReportsTruePercentage()
    {
        using var admin=await fixture.Login("admin@example.test");var id=await Create(admin);using var c=await fixture.Login("faculty@example.test");var detail=await c.GetFromJsonAsync<JsonElement>(Path(id));var rows=detail.GetProperty("roster").EnumerateArray().ToArray();Assert.True(rows.Length>=2);var items=rows.Select((s,i)=>new{studentId=s.GetProperty("id").GetGuid(),status=i==0?"Absent":i==1?"Excused":"Present"}).ToArray();var request=new{version=1,items};Assert.Equal(HttpStatusCode.OK,(await c.PostAsJsonAsync(Path(id),request)).StatusCode);Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(Path(id),request)).StatusCode);
        var report=await c.GetFromJsonAsync<JsonElement>($"/api/v1/institutions/{fixture.Vit}/academic/report?studentId={items[0].studentId}");var subject=report.EnumerateArray().First();Assert.Equal(0,subject.GetProperty("present").GetInt32());Assert.True(subject.GetProperty("absent").GetInt32()>=1);Assert.Equal(0,subject.GetProperty("percentage").GetDecimal());
    }
    [Fact]public async Task PartialOrForeignRosterFailsAtomically()
    {
        using var c=await fixture.Login("admin@example.test");var id=await Create(c);var detail=await c.GetFromJsonAsync<JsonElement>(Path(id));var one=detail.GetProperty("roster")[0].GetProperty("id").GetGuid();Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Path(id),new{version=1,items=new[]{new{studentId=one,status="Present"}}})).StatusCode);using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();Assert.Equal(0,await db.AttendanceRecords.CountAsync(x=>x.ClassSessionId==id));Assert.False(await db.ClassSessions.Where(x=>x.Id==id).Select(x=>x.AttendanceSubmitted).SingleAsync());
    }
    [Fact]public async Task CorrectionRequiresSeparateReviewerAndCannotBeReplayed()
    {
        using var admin=await fixture.Login("admin@example.test");var id=await Create(admin);using var faculty=await fixture.Login("faculty@example.test");var detail=await faculty.GetFromJsonAsync<JsonElement>(Path(id));var items=detail.GetProperty("roster").EnumerateArray().Select(x=>new{studentId=x.GetProperty("id").GetGuid(),status="Present"}).ToArray();(await faculty.PostAsJsonAsync(Path(id),new{version=1,items})).EnsureSuccessStatusCode();detail=await faculty.GetFromJsonAsync<JsonElement>(Path(id));var recordId=detail.GetProperty("records")[0].GetProperty("id").GetGuid();var correction=await(await faculty.PostAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/academic/corrections",new{attendanceRecordId=recordId,newStatus="Absent",reason="Student left before the session"})).Content.ReadFromJsonAsync<JsonElement>();var correctionId=correction.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Forbidden,(await faculty.PostAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/academic/corrections/{correctionId}/review",new{approve=true,note="Self review"})).StatusCode);Assert.Equal(HttpStatusCode.OK,(await admin.PostAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/academic/corrections/{correctionId}/review",new{approve=true,note="Verified faculty request"})).StatusCode);Assert.Equal(HttpStatusCode.Conflict,(await admin.PostAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/academic/corrections/{correctionId}/review",new{approve=true,note="Replay"})).StatusCode);
        var own=await(await admin.PostAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/academic/corrections",new{attendanceRecordId=recordId,newStatus="Excused",reason="Administrator initiated request"})).Content.ReadFromJsonAsync<JsonElement>();Assert.Equal(HttpStatusCode.Forbidden,(await admin.PostAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/academic/corrections/{own.GetProperty("id").GetGuid()}/review",new{approve=true,note="Own request"})).StatusCode);
    }
    [Fact]public async Task UnassignedAndForeignClassesAreDenied()
    {
        using var admin=await fixture.Login("admin@example.test");var d=await Setup();using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();var other=await db.Faculty.Where(x=>x.InstitutionId==fixture.Vit&&x.UserId==null&&!x.IsArchived).Select(x=>x.Id).FirstAsync();var start=new DateTimeOffset(2026,8,4,6,0,0,TimeSpan.Zero);var created=await(await admin.PostAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/academic/classes",new{subjectId=d.Subject,facultyId=other,classroomId=d.Room,section=d.Section,startsAt=start,endsAt=start.AddHours(1)})).Content.ReadFromJsonAsync<JsonElement>();using var faculty=await fixture.Login("faculty@example.test");Assert.Equal(HttpStatusCode.Forbidden,(await faculty.GetAsync(Path(created.GetProperty("id").GetGuid()))).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await faculty.GetAsync($"/api/v1/institutions/{fixture.Vdc}/academic/classes")).StatusCode);
    }
    [Fact]public async Task CancellationPreservesRosterAndFreesBooking()
    {
        using var c=await fixture.Login("admin@example.test");var start=new DateTimeOffset(2026,8,5,6,0,0,TimeSpan.Zero);var id=await Create(c,start);Assert.Equal(HttpStatusCode.NoContent,(await c.PostAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/academic/classes/{id}/cancel",new{version=1,reason="Cancelled test period"})).StatusCode);await Create(c,start);var detail=await c.GetFromJsonAsync<JsonElement>(Path(id));Assert.True(detail.GetProperty("session").GetProperty("isCancelled").GetBoolean());Assert.True(detail.GetProperty("roster").GetArrayLength()>0);
    }
    [Fact]public async Task DemoPortalLinkIsDeterministicEvenWithEarlierExtraProfiles()
    {
        using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();var account=await db.Users.SingleAsync(x=>x.Email=="faculty@example.test");var expected=await db.Faculty.SingleAsync(x=>x.InstitutionId==fixture.Vit&&x.Email=="faculty-vit-1@example.test");var extra=new VHEI.Domain.Faculty{InstitutionId=fixture.Vit,FullName="Demo Link Regression",Email="aaa-demo-link@example.test",Designation="DEMO Lecturer",IsDemo=true};db.Faculty.Add(extra);expected.UserId=null;await db.SaveChangesAsync();extra.UserId=account.Id;await db.SaveChangesAsync();await AcademicDemoSeed.RunAsync(db);Assert.Equal(account.Id,expected.UserId);Assert.Null(extra.UserId);Assert.True(await db.AuditLogs.AnyAsync(x=>x.InstitutionId==fixture.Vit&&x.Action=="Demo.FacultyAccountLinked"));
    }
}
