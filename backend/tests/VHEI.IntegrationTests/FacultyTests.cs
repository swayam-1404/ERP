using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
namespace VHEI.IntegrationTests;
[Collection("PostgresCore")]
public class FacultyTests(CoreFixture fixture)
{
    [Fact]public async Task FacultyEditVersionAndArchiveAreEnforced()
    {
        using var c=await fixture.Login("institution@example.test");var created=await(await c.PostAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/faculty",new{fullName="Faculty Lifecycle Demo",email="faculty-lifecycle@example.test",designation="DEMO Lecturer"})).Content.ReadFromJsonAsync<JsonElement>();var id=created.GetProperty("id").GetGuid();
        var edit=new{fullName="Faculty Revised Demo",email="faculty-lifecycle@example.test",designation="DEMO Senior Lecturer",version=1};Assert.Equal(HttpStatusCode.OK,(await c.PutAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/faculty/{id}",edit)).StatusCode);Assert.Equal(HttpStatusCode.Conflict,(await c.PutAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/faculty/{id}",edit)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await c.PostAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/faculty/{id}/archive",new{version=2,reason="Lifecycle test"})).StatusCode);var list=await c.GetFromJsonAsync<JsonElement>($"/api/v1/institutions/{fixture.Vit}/faculty?search=Faculty%20Revised");Assert.Equal(0,list.GetProperty("total").GetInt32());
    }
    [Fact]public async Task FacultyCannotEditAndForeignInstitutionCannotArchive()
    {
        using var admin=await fixture.Login("admin@example.test");var list=await admin.GetFromJsonAsync<JsonElement>($"/api/v1/institutions/{fixture.Vdc}/faculty");var id=list.GetProperty("items")[0].GetProperty("id").GetGuid();using var c=await fixture.Login("institution@example.test");Assert.Equal(HttpStatusCode.NotFound,(await c.PostAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/faculty/{id}/archive",new{version=1,reason="Tamper"})).StatusCode);
        using var faculty=await fixture.Login("faculty@example.test");Assert.Equal(HttpStatusCode.Forbidden,(await faculty.PutAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/faculty/{id}",new{fullName="Tamper",email="tamper@example.test",designation="Demo",version=1})).StatusCode);
    }
}
