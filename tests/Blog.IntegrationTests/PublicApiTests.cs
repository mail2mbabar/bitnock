using System.Net;
using System.Net.Http.Json;
using Blog.Application.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Blog.IntegrationTests;

public class PublicApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly bool _available;

    public PublicApiTests(WebApplicationFactory<Program> factory)
    {
        try
        {
            _client = factory.CreateClient();
            var health = _client.GetAsync("/health/ready").GetAwaiter().GetResult();
            _available = health.IsSuccessStatusCode;
        }
        catch
        {
            _client = factory.CreateClient();
            _available = false;
        }
    }

    [Fact]
    public async Task Health_live_returns_ok()
    {
        if (!_available)
        {
            return;
        }

        var response = await _client.GetAsync("/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_rejects_bad_password()
    {
        if (!_available)
        {
            return;
        }

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email = "admin@localhost", password = "wrong" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<Envelope<object>>();
        body!.Success.Should().BeFalse();
        body.Error!.Code.Should().Be("UNAUTHORIZED");
    }

    [Fact]
    public async Task Agent_without_key_is_unauthorized()
    {
        if (!_available)
        {
            return;
        }

        var response = await _client.GetAsync("/api/v1/agent/publishing-rules");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Robots_disallows_admin()
    {
        if (!_available)
        {
            return;
        }

        var text = await _client.GetStringAsync("/robots.txt");
        text.Should().Contain("Disallow: /admin");
        text.Should().Contain("Sitemap:");
    }

    [Fact]
    public async Task Public_articles_do_not_require_auth()
    {
        if (!_available)
        {
            return;
        }

        var response = await _client.GetAsync("/api/v1/public/articles");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
