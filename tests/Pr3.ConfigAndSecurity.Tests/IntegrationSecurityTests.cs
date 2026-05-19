using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Pr3.ConfigAndSecurity.Tests;

public class IntegrationSecurityTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public IntegrationSecurityTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("AppSettings:Mode", "Study");
            builder.UseSetting("AppSettings:TrustedOrigins", "http://trusted.example.com");
            builder.UseSetting("AppSettings:Port", "5000");
            builder.UseSetting("AppSettings:ReadLimitPerMinute", "3");
            builder.UseSetting("AppSettings:CreateLimitPerMinute", "2");
        });
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    [Fact]
    public async Task UntrustedOrigin_IsBlocked()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/items");
        request.Headers.Add("Origin", "http://evil.com");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TrustedOrigin_IsAllowed()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/items");
        request.Headers.Add("Origin", "http://trusted.example.com");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RateLimit_ReadBlocksAfterLimit()
    {
        var client = _factory.CreateClient();
        for (int i = 0; i < 3; i++)
        {
            var r = await client.GetAsync("/items");
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        }

        var blocked = await client.GetAsync("/items");
        Assert.Equal((HttpStatusCode)429, blocked.StatusCode);
    }

    [Fact]
    public async Task RateLimit_CreateBlocksAfterLimit()
    {
        var client = _factory.CreateClient();
        for (int i = 0; i < 2; i++)
        {
            var r = await client.PostAsJsonAsync("/items", new { Name = $"Item {i}" });
            Assert.True(r.StatusCode == HttpStatusCode.Created || r.StatusCode == HttpStatusCode.OK);
        }

        var blocked = await client.PostAsJsonAsync("/items", new { Name = "Overflow" });
        Assert.Equal((HttpStatusCode)429, blocked.StatusCode);
    }

    [Fact]
    public async Task SecurityHeaders_ArePresent()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/items");

        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").First());

        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").First());
    }

    [Fact]
    public async Task StudyMode_ReturnsDetailedError()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/items/999");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("not found", body, StringComparison.OrdinalIgnoreCase);
    }
}
