using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;
namespace VHEI.IntegrationTests;
public class FoundationTests
{
    private static WebApplicationFactory<Program> Factory(string connection) => new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string,string?> { ["ConnectionStrings:Erp"] = connection, ["Seed:Mode"]="None" })));
    [Fact] public async Task LivenessDoesNotRequireDatabase()
    {
        await using var factory = Factory("Host=127.0.0.1;Port=1;Database=unavailable;Username=test;Password=test;Timeout=1");
        var response = await factory.CreateClient().GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);
    }
    [Fact] public async Task ReadinessFailsWhenDatabaseUnavailableWithoutLeakingSecrets()
    {
        await using var factory = Factory("Host=127.0.0.1;Port=1;Database=unavailable;Username=test;Password=secret-marker;Timeout=1");
        var response = await factory.CreateClient().GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.ServiceUnavailable,response.StatusCode);
        Assert.DoesNotContain("secret-marker",await response.Content.ReadAsStringAsync());
    }
}
