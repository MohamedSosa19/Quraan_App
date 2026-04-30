using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quraan.Application.Auth;
using Quraan.Infrastructure.Persistence;
using Xunit;

namespace Quraan.ContractTests;

/// <summary>
/// T141 — Wire-shape contracts for /auth/*. Uses an in-memory store; the real
/// flow lives in IntegrationTests/Auth/.
/// </summary>
public sealed class AuthContractTests : IClassFixture<AuthContractTests.Factory>
{
    public sealed class Factory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = "contract-auth-" + Guid.NewGuid();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<QuraanDbContext>));
                services.AddDbContext<QuraanDbContext>(o =>
                    o.UseInMemoryDatabase(_dbName));
            });
        }
    }

    private readonly Factory _factory;
    public AuthContractTests(Factory factory) => _factory = factory;

    [Fact]
    public async Task POST_register_returns_201_with_AuthResponse_and_Set_Cookie()
    {
        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync(new Uri("/api/v1/auth/register", UriKind.Relative),
            new RegisterRequestDto("alice@example.test", "passwordpassword", "Alice", "en"));
        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        resp.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        var refreshCookie = cookies!.FirstOrDefault(c => c.StartsWith("quraan-refresh=", StringComparison.Ordinal));
        refreshCookie.Should().NotBeNull();
        refreshCookie!.Should().Contain("httponly", because: "refresh cookie must be HttpOnly");
        refreshCookie.Should().Contain("secure", because: "refresh cookie must be Secure");
        refreshCookie.Should().Contain("samesite=strict", because: "refresh cookie must be SameSite=Strict");

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        foreach (var prop in new[] { "accessToken", "expiresInSeconds", "user" })
            doc.RootElement.TryGetProperty(prop, out _).Should().BeTrue($"AuthResponse requires '{prop}'");
        doc.RootElement.GetProperty("expiresInSeconds").GetInt32().Should().Be(900);
    }

    [Fact]
    public async Task POST_register_with_existing_email_returns_409_problem_details()
    {
        using var client = _factory.CreateClient();
        await client.PostAsJsonAsync(new Uri("/api/v1/auth/register", UriKind.Relative),
            new RegisterRequestDto("dup@example.test", "passwordpassword", null, null));

        var resp = await client.PostAsJsonAsync(new Uri("/api/v1/auth/register", UriKind.Relative),
            new RegisterRequestDto("dup@example.test", "passwordpassword", null, null));
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        resp.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task POST_login_with_unknown_email_returns_401_problem_details()
    {
        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync(new Uri("/api/v1/auth/login", UriKind.Relative),
            new LoginRequestDto("nobody@example.test", "anything"));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        resp.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task POST_logout_returns_204_and_clears_the_refresh_cookie()
    {
        using var client = _factory.CreateClient();
        var resp = await client.PostAsync(new Uri("/api/v1/auth/logout", UriKind.Relative), content: null);
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
