using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VHEI.Domain;
using VHEI.Infrastructure;
using Xunit;
namespace VHEI.IntegrationTests;
[Collection("PostgresCore")]
public class StudentTests(CoreFixture fixture)
{
    private async Task<object> Request(Guid institution,string number)
    {
        using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        return new { studentNumber=number,fullName="Test Student Demo",email="student-test@example.test",status="Active",programmeId=await db.Programmes.Where(x=>x.InstitutionId==institution).Select(x=>x.Id).FirstAsync(),academicSessionId=await db.AcademicSessions.Where(x=>x.InstitutionId==institution).Select(x=>x.Id).FirstAsync(),batchYear=2026,semester=1,section="A",guardianName="Test Guardian Demo",guardianEmail="guardian-test@example.test",version=1 };
    }
    [Fact] public async Task SeedIs500SyntheticAndIdempotent()
    {
        using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        Assert.Equal(500,await db.Students.CountAsync(x=>x.StudentNumber.StartsWith("DEMO-")));
        Assert.All(await db.Students.Where(x=>x.StudentNumber.StartsWith("DEMO-")).ToListAsync(),s=>{Assert.True(s.IsDemo);Assert.EndsWith("@example.test",s.Email);});
        var before=await db.Students.CountAsync();await DemoSeed.RunAsync(db);Assert.Equal(before,await db.Students.CountAsync());
    }
    [Fact] public async Task CreationPersistsGuardianEnrollmentAndAudit()
    {
        using var c=await fixture.Login("institution@example.test");var r=await c.PostAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/students",await Request(fixture.Vit,"TEST-CREATE"));Assert.Equal(HttpStatusCode.Created,r.StatusCode);
        var created=await r.Content.ReadFromJsonAsync<JsonElement>();var detail=await c.GetFromJsonAsync<JsonElement>($"/api/v1/institutions/{fixture.Vit}/students/{created.GetProperty("id").GetGuid()}");Assert.Equal("Test Guardian Demo",detail.GetProperty("guardian").GetProperty("fullName").GetString());Assert.Equal(1,detail.GetProperty("enrollment").GetProperty("semester").GetInt32());
        using var scope=fixture.App.Services.CreateScope();Assert.True(await scope.ServiceProvider.GetRequiredService<ErpDbContext>().AuditLogs.AnyAsync(x=>x.Action=="Student.Created"&&x.Resource==created.GetProperty("id").GetString()));
    }
    [Fact] public async Task InvalidCreationLeavesNoOrphanRecords()
    {
        using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();var before=await db.Guardians.CountAsync();using var c=await fixture.Login("admin@example.test");var response=await c.PostAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/students",new{studentNumber="",fullName="x",email="bad",status="Unknown",programmeId=Guid.NewGuid(),academicSessionId=Guid.NewGuid(),batchYear=2026,semester=99,section="",guardianName="",guardianEmail="bad"});Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);Assert.Equal(before,await db.Guardians.CountAsync());
    }
    [Fact] public async Task ForeignResourceIsNotFoundAndForeignProgrammeRejected()
    {
        using var scope=fixture.App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();var foreignStudent=await db.Students.Where(x=>x.InstitutionId==fixture.Vdc).Select(x=>x.Id).FirstAsync();using var c=await fixture.Login("institution@example.test");Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"/api/v1/institutions/{fixture.Vit}/students/{foreignStudent}")).StatusCode);Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/students",await Request(fixture.Vdc,"TEST-FOREIGN"))).StatusCode);
    }
    [Fact] public async Task DuplicateStudentNumberIsConflict()
    {using var c=await fixture.Login("admin@example.test");var request=await Request(fixture.Vit,"TEST-DUPLICATE");Assert.Equal(HttpStatusCode.Created,(await c.PostAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/students",request)).StatusCode);Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/students",request)).StatusCode);}
    [Fact] public async Task ConcurrentEditAllowsExactlyOneWriter()
    {
        using var c=await fixture.Login("admin@example.test");var request=await Request(fixture.Vit,"TEST-CONCURRENT");var created=await (await c.PostAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/students",request)).Content.ReadFromJsonAsync<JsonElement>();var id=created.GetProperty("id").GetGuid();var responses=await Task.WhenAll(c.PutAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/students/{id}",request),c.PutAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/students/{id}",request));Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.OK);Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.Conflict);
    }
    [Fact] public async Task ArchiveRetainsHistoryAndRequiresReason()
    {
        using var c=await fixture.Login("institution@example.test");var created=await (await c.PostAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/students",await Request(fixture.Vit,"TEST-ARCHIVE"))).Content.ReadFromJsonAsync<JsonElement>();var id=created.GetProperty("id").GetGuid();Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/students/{id}/archive",new{version=1,reason=""})).StatusCode);Assert.Equal(HttpStatusCode.NoContent,(await c.PostAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/students/{id}/archive",new{version=1,reason="Duplicate demo registration"})).StatusCode);var detail=await c.GetFromJsonAsync<JsonElement>($"/api/v1/institutions/{fixture.Vit}/students/{id}");Assert.Equal("Archived",detail.GetProperty("student").GetProperty("status").GetString());Assert.True(detail.GetProperty("enrollment").TryGetProperty("id",out _));
    }
    [Fact] public async Task FacultyCannotReadPrivateStudentsOrExport()
    {using var c=await fixture.Login("faculty@example.test");Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync($"/api/v1/institutions/{fixture.Vit}/students")).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync($"/api/v1/institutions/{fixture.Vit}/students/export")).StatusCode);}
    [Fact] public async Task FilterAndExportRespectInstitutionScope()
    {using var c=await fixture.Login("institution@example.test");var rows=await c.GetFromJsonAsync<JsonElement>($"/api/v1/institutions/{fixture.Vit}/students?pageSize=100&search=DEMO-VIT&status=Active");Assert.All(rows.GetProperty("items").EnumerateArray(),s=>{Assert.StartsWith("DEMO-VIT-",s.GetProperty("studentNumber").GetString());Assert.Equal("Active",s.GetProperty("status").GetString());});var export=await c.GetStringAsync($"/api/v1/institutions/{fixture.Vit}/students/export");Assert.Contains("DEMO-VIT-",export);Assert.DoesNotContain("DEMO-VDC-",export);}
}
