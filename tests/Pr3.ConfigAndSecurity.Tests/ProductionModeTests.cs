using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Pr3.ConfigAndSecurity.Tests;

public class ProductionModeTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProductionModeTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("AppSettings:Mode", "Production");
            builder.UseSetting("AppSettings:TrustedOrigins", "http://trusted.example.com");
            builder.UseSetting("AppSettings:Port", "5000");
            builder.UseSetting("AppSettings:ReadLimitPerMinute", "60");
            builder.UseSetting("AppSettings:CreateLimitPerMinute", "10");
        });
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    [Fact]
    public async Task ProductionMode_ReturnsMinimalError()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/items/999");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Not found.", body);
        Assert.DoesNotContain("id", body);
    }
}
