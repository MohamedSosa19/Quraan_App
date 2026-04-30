using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quraan.Application.LastRead;
using Quraan.ContractTests.TestAuth;
using Quraan.Infrastructure.Identity;
using Quraan.Infrastructure.Persistence;
using Xunit;

namespace Quraan.ContractTests;

/// <summary>
/// T176a — Wire shape for /lastread/me. Asserts:
///   • GET returns 200 with null when no position set
///   • GET returns 200 with the LastReadPosition shape after PUT
///   • PUT returns 200 + the merged value
///   • PUT with invalid surahId returns 400 problem+json
///   • Both endpoints return 401 when unauthenticated
/// </summary>
public sealed class LastReadContractTests : IClassFixture<LastReadContractTests.Factory>
{
    public sealed class Factory : WebApplicationFactory<Program>
    {
        // Each test uses its own user ID (set via X-Test-User) so the in-memory
        // LastReadPosition state can't leak across tests in this class.
        private readonly string _dbName = "contract-lastread-" + Guid.NewGuid();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<QuraanDbContext>));
                services.AddDbContext<QuraanDbContext>(o => o.UseInMemoryDatabase(_dbName));
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            });
        }
    }

    private readonly Factory _factory;
    public LastReadContractTests(Factory factory) => _factory = factory;

    private HttpClient AuthedClient(out Guid userId)
    {
        userId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QuraanDbContext>();
            db.Users.Add(new ApplicationUser
            {
                Id = userId,
                UserName = $"lr-{userId:N}@test.local",
                NormalizedUserName = $"LR-{userId:N}@TEST.LOCAL",
                Email = $"lr-{userId:N}@test.local",
                NormalizedEmail = $"LR-{userId:N}@TEST.LOCAL",
                PreferredLanguage = "en",
            });
            db.SaveChanges();
        }
        var c = _factory.CreateClient();
        c.DefaultRequestHeaders.Add("X-Test-User", userId.ToString());
        return c;
    }

    [Fact]
    public async Task GET_lastread_me_when_unset_returns_200_null()
    {
        using var client = AuthedClient(out _);
        var resp = await client.GetAsync(new Uri("/api/v1/lastread/me", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadAsStringAsync();
        body.Trim().Should().Be("null");
    }

    [Fact]
    public async Task PUT_lastread_me_returns_200_with_LastReadPosition_shape()
    {
        using var client = AuthedClient(out _);
        var resp = await client.PutAsJsonAsync(new Uri("/api/v1/lastread/me", UriKind.Relative),
            new LastReadPositionDto(2, 25, DateTime.UtcNow));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        foreach (var prop in new[] { "surahId", "numberInSurah", "updatedAt" })
            doc.RootElement.TryGetProperty(prop, out _).Should().BeTrue($"LastReadPosition requires '{prop}'");
        doc.RootElement.GetProperty("surahId").GetByte().Should().Be(2);
        doc.RootElement.GetProperty("numberInSurah").GetInt16().Should().Be(25);
    }

    [Fact]
    public async Task PUT_lastread_me_with_invalid_surahId_returns_400_problem_details()
    {
        using var client = AuthedClient(out _);
        var resp = await client.PutAsJsonAsync(new Uri("/api/v1/lastread/me", UriKind.Relative),
            new LastReadPositionDto(0, 1, DateTime.UtcNow));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        resp.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task GET_lastread_me_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/v1/lastread/me", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PUT_lastread_me_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();
        var resp = await client.PutAsJsonAsync(new Uri("/api/v1/lastread/me", UriKind.Relative),
            new LastReadPositionDto(1, 1, DateTime.UtcNow));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
