using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using VHEI.Infrastructure;
using Xunit;
namespace VHEI.IntegrationTests;
public class CoreFixture : IAsyncLifetime
{
    public WebApplicationFactory<Program> App { get; private set; } = null!;
    public const string Password="Test-Development-Only!234";
    public Guid Vit { get; private set; }
    public Guid Vdc { get; private set; }
    private string database="";
    public async Task InitializeAsync()
    {
        var config=new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("TEST_DATABASE")??throw new InvalidOperationException("TEST_DATABASE is required. Tests use isolated PostgreSQL."));
        database="vhei_integration_test_"+Guid.NewGuid().ToString("N");config.Database=database;config.CommandTimeout=120;
        App=new WebApplicationFactory<Program>().WithWebHostBuilder(b=>b.UseEnvironment("Development").ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{["ConnectionStrings:Erp"]=config.ConnectionString,["Seed:Mode"]="Demo",["Seed:AdminPassword"]=Password,["RateLimiting:LoginPermitLimit"]="50"})));
        using var startup=App.CreateClient();using var scope=App.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        Vit=await db.Institutions.Where(x=>x.Code=="VIT").Select(x=>x.Id).SingleAsync();Vdc=await db.Institutions.Where(x=>x.Code=="VDC").Select(x=>x.Id).SingleAsync();
    }
    public async Task<HttpClient> Login(string email)
    {
        var client=App.CreateClient(new WebApplicationFactoryClientOptions{AllowAutoRedirect=false,BaseAddress=new Uri("https://localhost")});await Csrf(client);
        (await client.PostAsJsonAsync("/api/v1/auth/login",new{email,password=Password})).EnsureSuccessStatusCode();await Csrf(client);return client;
    }
    public static async Task Csrf(HttpClient client)
    {
        var body=await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/csrf");client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");client.DefaultRequestHeaders.Add("X-CSRF-TOKEN",body.GetProperty("token").GetString());
    }
    public async Task DisposeAsync()
    {
        if(!database.StartsWith("vhei_integration_test_",StringComparison.Ordinal))throw new InvalidOperationException("Unsafe cleanup target.");
        using var scope=App.Services.CreateScope();await scope.ServiceProvider.GetRequiredService<ErpDbContext>().Database.EnsureDeletedAsync();await App.DisposeAsync();
    }
}
[CollectionDefinition("PostgresCore")] public class CoreCollection:ICollectionFixture<CoreFixture>{}
[Collection("PostgresCore")]
public class CoreSecurityTests(CoreFixture fixture)
{
    [Fact] public async Task AnonymousCannotReadInstitutions(){using var c=fixture.App.CreateClient();Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync("/api/v1/institutions")).StatusCode);}
    [Fact] public async Task ForeignInstitutionDenied(){using var c=await fixture.Login("institution@example.test");Assert.Equal(HttpStatusCode.OK,(await c.GetAsync($"/api/v1/institutions/{fixture.Vit}/reference")).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync($"/api/v1/institutions/{fixture.Vdc}/reference")).StatusCode);}
    [Fact] public async Task FacultyCannotWrite(){using var c=await fixture.Login("faculty@example.test");Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/departments",new{name="Tampered Department"})).StatusCode);}
    [Fact] public async Task MutationRequiresCsrf(){using var c=await fixture.Login("admin@example.test");c.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/departments",new{name="Missing CSRF"})).StatusCode);}
    [Fact] public async Task MasterWritePersistsWithAudit(){using var c=await fixture.Login("institution@example.test");Assert.Equal(HttpStatusCode.Created,(await c.PostAsJsonAsync($"/api/v1/institutions/{fixture.Vit}/departments",new{name="DEMO Applied Studies"})).StatusCode);var audit=await c.GetFromJsonAsync<JsonElement>($"/api/v1/institutions/{fixture.Vit}/audit");Assert.Contains(audit.GetProperty("items").EnumerateArray(),x=>x.GetProperty("action").GetString()=="Department.Created");}
    [Fact] public async Task ForeignMutationCreatesNothing(){using var c=await fixture.Login("institution@example.test");Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync($"/api/v1/institutions/{fixture.Vdc}/departments",new{name="Cross Tenant"})).StatusCode);using var scope=fixture.App.Services.CreateScope();Assert.False(await scope.ServiceProvider.GetRequiredService<ErpDbContext>().Departments.AnyAsync(x=>x.Name=="Cross Tenant"));}
    [Fact] public async Task CookieHttpOnlyAndLogoutEndsAccess(){using var c=fixture.App.CreateClient(new WebApplicationFactoryClientOptions{BaseAddress=new Uri("https://localhost")});await CoreFixture.Csrf(c);var login=await c.PostAsJsonAsync("/api/v1/auth/login",new{email="admin@example.test",password=CoreFixture.Password});Assert.Contains(login.Headers.GetValues("Set-Cookie"),h=>h.StartsWith("vhei.session=")&&h.Contains("httponly",StringComparison.OrdinalIgnoreCase));await CoreFixture.Csrf(c);Assert.Equal(HttpStatusCode.NoContent,(await c.PostAsync("/api/v1/auth/logout",null)).StatusCode);Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync("/api/v1/auth/me")).StatusCode);}
}
