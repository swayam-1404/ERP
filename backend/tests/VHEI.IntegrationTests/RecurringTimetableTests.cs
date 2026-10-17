using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VHEI.Api;
using VHEI.Infrastructure;
using Xunit;
namespace VHEI.IntegrationTests;

[CollectionDefinition("PostgresRecurring")] public class RecurringCollection:ICollectionFixture<CoreFixture>{}
[Collection("PostgresRecurring")]
public class RecurringTimetableTests(CoreFixture fixture)
{
    private string Base(Guid? institution=null)=>$"/api/v1/institutions/{institution??fixture.Vit}/academic";
    private async Task<SeriesRequest> Request(DateOnly first,DateOnly last)
    {
        using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();var subject=await db.Subjects.FirstAsync(x=>x.InstitutionId==fixture.Vit);
        var section=await db.Enrollments.Where(x=>x.InstitutionId==fixture.Vit&&x.ProgrammeId==subject.ProgrammeId&&x.AcademicSessionId==subject.AcademicSessionId&&x.Semester==subject.Semester).Select(x=>x.Section).FirstAsync();
        return new("DEMO series "+Guid.NewGuid(),subject.Id,await db.Faculty.Where(x=>x.InstitutionId==fixture.Vit&&x.UserId!=null&&!x.IsArchived).Select(x=>x.Id).FirstAsync(),await db.Classrooms.Where(x=>x.InstitutionId==fixture.Vit).Select(x=>x.Id).FirstAsync(),section,first,last,new(9,0),new(10,0),[(int)first.DayOfWeek],[]);
    }
    private async Task<Guid> Create(HttpClient c,SeriesRequest r)
    {
        var response=await c.PostAsJsonAsync(Base()+"/series",r);Assert.Equal(HttpStatusCode.Created,response.StatusCode);return(await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }
    [Fact]public async Task PreviewUsesIstSkipsExplicitDatesAndSavesFrozenRosters()
    {
        using var c=await fixture.Login("admin@example.test");var r=(await Request(new(2030,1,7),new(2030,1,21))) with{ExcludedDates=[new(2030,1,14)]};
        var preview=await(await c.PostAsJsonAsync(Base()+"/series/preview",r)).Content.ReadFromJsonAsync<JsonElement>();Assert.False(preview.GetProperty("hasConflicts").GetBoolean());Assert.Equal(2,preview.GetProperty("periods").GetArrayLength());Assert.Equal(new DateTime(2030,1,7,3,30,0,DateTimeKind.Utc),preview.GetProperty("periods")[0].GetProperty("startsAt").GetDateTime());
        using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();Assert.False(await db.TimetableSeries.AnyAsync(x=>x.Name==r.Name));
        var id=await Create(c,r);var periods=await db.ClassSessions.Where(x=>x.TimetableSeriesId==id).ToListAsync();Assert.Equal(2,periods.Count);var size=preview.GetProperty("rosterCount").GetInt32();Assert.True(size>0);foreach(var p in periods)Assert.Equal(size,await db.ClassRosters.CountAsync(x=>x.ClassSessionId==p.Id));Assert.True(await db.AuditLogs.AnyAsync(x=>x.Action=="TimetableSeries.Created"&&x.Resource==id.ToString()));
    }
    [Fact]public async Task StalePreviewAndMiddleConflictRollBackWholeSeries()
    {
        using var c=await fixture.Login("admin@example.test");var r=await Request(new(2030,2,4),new(2030,2,18));var preview=await(await c.PostAsJsonAsync(Base()+"/series/preview",r)).Content.ReadFromJsonAsync<JsonElement>();Assert.False(preview.GetProperty("hasConflicts").GetBoolean());
        var start=new DateTimeOffset(2030,2,11,9,0,0,TimeSpan.FromMinutes(330));(await c.PostAsJsonAsync(Base()+"/classes",new ClassRequest(r.SubjectId,r.FacultyId,r.ClassroomId,r.Section,start,start.AddHours(1)))).EnsureSuccessStatusCode();
        using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();var classes=await db.ClassSessions.CountAsync();var rosters=await db.ClassRosters.CountAsync();var audits=await db.AuditLogs.CountAsync();
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(Base()+"/series",r)).StatusCode);Assert.False(await db.TimetableSeries.AnyAsync(x=>x.Name==r.Name));Assert.Equal(classes,await db.ClassSessions.CountAsync());Assert.Equal(rosters,await db.ClassRosters.CountAsync());Assert.Equal(audits,await db.AuditLogs.CountAsync());
        var conflicted=await(await c.PostAsJsonAsync(Base()+"/series/preview",r)).Content.ReadFromJsonAsync<JsonElement>();Assert.True(conflicted.GetProperty("hasConflicts").GetBoolean());Assert.Contains(conflicted.GetProperty("periods")[1].GetProperty("conflicts").EnumerateArray(),x=>x.GetString()=="Classroom");
    }
    [Fact]public async Task ConcurrentSeriesCreationHasOneWinner()
    {
        using var c=await fixture.Login("institution@example.test");var r=await Request(new(2030,3,4),new(2030,3,18));var responses=await Task.WhenAll(c.PostAsJsonAsync(Base()+"/series",r),c.PostAsJsonAsync(Base()+"/series",r));Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.Created);Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.Conflict);
        using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();var series=await db.TimetableSeries.SingleAsync(x=>x.Name==r.Name);Assert.Equal(3,await db.ClassSessions.CountAsync(x=>x.TimetableSeriesId==series.Id));
    }
    [Fact]public async Task InvalidPatternsRangesAndSkippedDatesCreateNothing()
    {
        using var c=await fixture.Login("institution@example.test");var r=await Request(new(2030,4,1),new(2030,4,15));
        var invalid=new[]{r with{DaysOfWeek=[]},r with{DaysOfWeek=[1,1]},r with{DaysOfWeek=[7]},r with{LastDate=r.FirstDate.AddDays(112)},r with{EndTime=new(8,0)},r with{ExcludedDates=[r.FirstDate.AddDays(1)]},r with{ExcludedDates=[r.FirstDate,r.FirstDate]},r with{DaysOfWeek=[0,1,2,3,4,5,6],LastDate=r.FirstDate.AddDays(100)},r with{FirstDate=r.LastDate.AddDays(1)}};
        foreach(var body in invalid)Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Base()+"/series",body)).StatusCode);
        using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();Assert.False(await db.TimetableSeries.AnyAsync(x=>x.Name==r.Name));
    }
    [Fact]public async Task CancellationPreservesPastAttendanceAndRejectsStaleReplay()
    {
        using var admin=await fixture.Login("admin@example.test");var today=DateOnly.FromDateTime(DateTime.UtcNow.AddMinutes(330));var r=(await Request(today.AddDays(-7),today.AddDays(7))) with{StartTime=new(23,0),EndTime=new(23,1)};var id=await Create(admin,r);
        using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();var past=await db.ClassSessions.Where(x=>x.TimetableSeriesId==id).OrderBy(x=>x.StartsAt).FirstAsync();var roster=await db.ClassRosters.Where(x=>x.ClassSessionId==past.Id).Select(x=>x.StudentId).ToListAsync();
        (await admin.PostAsJsonAsync(Base()+$"/classes/{past.Id}/attendance",new AttendanceRequest(1,roster.Select(x=>new AttendanceItem(x,"Present")).ToArray()))).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.BadRequest,(await admin.PostAsJsonAsync(Base()+$"/series/{id}/cancel",new ArchiveRequest(1,""))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await admin.PostAsJsonAsync(Base()+$"/series/{id}/cancel",new ArchiveRequest(99,"Wrong version"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await admin.PostAsJsonAsync(Base()+$"/series/{id}/cancel",new ArchiveRequest(1,"Future teaching dates withdrawn"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await admin.PostAsJsonAsync(Base()+$"/series/{id}/cancel",new ArchiveRequest(1,"Replayed cancellation"))).StatusCode);
        await db.Entry(past).ReloadAsync();Assert.False(past.IsCancelled);Assert.True(past.AttendanceSubmitted);Assert.Equal(roster.Count,await db.AttendanceRecords.CountAsync(x=>x.ClassSessionId==past.Id));Assert.True(await db.ClassSessions.AnyAsync(x=>x.TimetableSeriesId==id&&x.IsCancelled));Assert.Equal(3,await db.ClassSessions.CountAsync(x=>x.TimetableSeriesId==id));
    }
    [Fact]public async Task FacultyForeignResourcesAndCsrfRemainProtected()
    {
        using var admin=await fixture.Login("admin@example.test");var r=await Request(new(2030,5,6),new(2030,5,20));var id=await Create(admin,r);using var faculty=await fixture.Login("faculty@example.test");
        Assert.Equal(HttpStatusCode.Forbidden,(await faculty.GetAsync(Base()+"/series")).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await faculty.PostAsJsonAsync(Base()+"/series/preview",r)).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await faculty.PostAsJsonAsync(Base()+"/series",r)).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await faculty.PostAsJsonAsync(Base()+$"/series/{id}/cancel",new ArchiveRequest(1,"Not an administrator"))).StatusCode);
        using var institution=await fixture.Login("institution@example.test");Assert.Equal(HttpStatusCode.Forbidden,(await institution.GetAsync(Base(fixture.Vdc)+"/series")).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await admin.GetAsync(Base(fixture.Vdc)+$"/series/{id}")).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await admin.PostAsJsonAsync(Base(fixture.Vdc)+$"/series/{id}/cancel",new ArchiveRequest(1,"Wrong institution"))).StatusCode);
        admin.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");Assert.Equal(HttpStatusCode.BadRequest,(await admin.PostAsJsonAsync(Base()+"/series",r)).StatusCode);
    }
    [Fact]public async Task ForeignResourcesCapacityAndEmptyRosterAreRejected()
    {
        using var c=await fixture.Login("admin@example.test");var r=await Request(new(2030,6,3),new(2030,6,17));using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        var foreign=await db.Classrooms.Where(x=>x.InstitutionId==fixture.Vdc).Select(x=>x.Id).FirstAsync();Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Base()+"/series/preview",r with{ClassroomId=foreign})).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Base()+"/series",r with{Section="NO-ROSTER"})).StatusCode);
        var tiny=new VHEI.Domain.Classroom{InstitutionId=fixture.Vit,Name="DEMO tiny recurring test room",Capacity=1,IsDemo=true};db.Classrooms.Add(tiny);await db.SaveChangesAsync();Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Base()+"/series",r with{ClassroomId=tiny.Id})).StatusCode);
        var id=await Create(c,r);var classes=await c.GetFromJsonAsync<JsonElement>(Base()+"/classes?from=2030-06-01T00:00:00Z&to=2030-07-01T00:00:00Z");Assert.Equal(3,classes.GetProperty("total").GetInt32());Assert.All(classes.GetProperty("items").EnumerateArray(),x=>Assert.Equal(id,x.GetProperty("timetableSeriesId").GetGuid()));
    }
}
