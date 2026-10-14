using System.Buffers.Binary;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VHEI.Domain;
using VHEI.Infrastructure;
using Xunit;
namespace VHEI.IntegrationTests;
[Collection("PostgresCore")]
public class MfaTests(CoreFixture fixture)
{
    private static string Code(string secret)
    {
        const string chars="ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";var bytes=new List<byte>();int buffer=0,bits=0;
        foreach(var c in secret){buffer=(buffer<<5)|chars.IndexOf(c);bits+=5;if(bits>=8){bits-=8;bytes.Add((byte)(buffer>>bits));}}
        Span<byte> counter=stackalloc byte[8];BinaryPrimitives.WriteInt64BigEndian(counter,DateTimeOffset.UtcNow.ToUnixTimeSeconds()/30);var hash=HMACSHA1.HashData(bytes.ToArray(),counter);int offset=hash[^1]&15;int number=(BinaryPrimitives.ReadInt32BigEndian(hash.AsSpan(offset,4))&int.MaxValue)%1000000;return number.ToString("D6");
    }
    [Fact] public async Task ProductionPrivilegedAccessRequiresCompletedMfa()
    {
        string connection;
        using(var scope=fixture.App.Services.CreateScope())
        {
            var users=scope.ServiceProvider.GetRequiredService<UserManager<ErpUser>>();var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();connection=scope.ServiceProvider.GetRequiredService<IConfiguration>().GetConnectionString("Erp")!;
            var user=new ErpUser{Id=Guid.NewGuid(),UserName="mfa@example.test",Email="mfa@example.test",EmailConfirmed=true,DisplayName="MFA Test Administrator"};Assert.True((await users.CreateAsync(user,CoreFixture.Password)).Succeeded);await users.AddToRoleAsync(user,"InstitutionAdministrator");db.InstitutionAssignments.Add(new InstitutionAssignment{UserId=user.Id,InstitutionId=fixture.Vit});await db.SaveChangesAsync();
        }
        await using var production=new WebApplicationFactory<Program>().WithWebHostBuilder(b=>b.UseEnvironment("Production").ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{["ConnectionStrings:Erp"]=connection,["Seed:Mode"]="None"})));
        using var client=production.CreateClient(new WebApplicationFactoryClientOptions{BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false});await CoreFixture.Csrf(client);
        (await client.PostAsJsonAsync("/api/v1/auth/login",new{email="mfa@example.test",password=CoreFixture.Password})).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/v1/institutions")).StatusCode);
        await CoreFixture.Csrf(client);var setup=await (await client.PostAsync("/api/v1/auth/mfa/setup",null)).Content.ReadFromJsonAsync<JsonElement>();var key=setup.GetProperty("key").GetString()!;
        (await client.PostAsJsonAsync("/api/v1/auth/mfa/enable",new{code=Code(key)})).EnsureSuccessStatusCode();await CoreFixture.Csrf(client);
        var login=await (await client.PostAsJsonAsync("/api/v1/auth/login",new{email="mfa@example.test",password=CoreFixture.Password})).Content.ReadFromJsonAsync<JsonElement>();Assert.True(login.GetProperty("requiresMfa").GetBoolean());
        (await client.PostAsJsonAsync("/api/v1/auth/mfa/login",new{code=Code(key)})).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/api/v1/institutions")).StatusCode);
    }
    [Fact] public void ProductionRejectsSyntheticSeedMode()
    {
        using var production=new WebApplicationFactory<Program>().WithWebHostBuilder(b=>b.UseEnvironment("Production").ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{["ConnectionStrings:Erp"]="Host=localhost;Database=unused;Username=unused",["Seed:Mode"]="Demo"})));
        var error=Assert.Throws<InvalidOperationException>(()=>production.CreateClient());Assert.Contains("forbidden",error.Message);
    }
    [Fact] public async Task LoginRateLimitRejectsRepeatedRequests()
    {
        string connection;using(var scope=fixture.App.Services.CreateScope())connection=scope.ServiceProvider.GetRequiredService<IConfiguration>().GetConnectionString("Erp")!;
        await using var app=new WebApplicationFactory<Program>().WithWebHostBuilder(b=>b.UseEnvironment("Development").ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{["ConnectionStrings:Erp"]=connection,["Seed:Mode"]="None",["RateLimiting:LoginPermitLimit"]="3"})));
        using var client=app.CreateClient();await CoreFixture.Csrf(client);for(int i=0;i<3;i++)Assert.Equal(HttpStatusCode.Unauthorized,(await client.PostAsJsonAsync("/api/v1/auth/login",new{email="unknown@example.test",password="Wrong-Test-Password"})).StatusCode);Assert.Equal(HttpStatusCode.TooManyRequests,(await client.PostAsJsonAsync("/api/v1/auth/login",new{email="unknown@example.test",password="Wrong-Test-Password"})).StatusCode);
    }
}
