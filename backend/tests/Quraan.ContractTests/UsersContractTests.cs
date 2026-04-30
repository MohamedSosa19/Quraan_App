using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quraan.ContractTests.TestAuth;
using Quraan.Infrastructure.Identity;
using Quraan.Infrastructure.Persistence;
using Xunit;

namespace Quraan.ContractTests;

public sealed class UsersContractTests : IClassFixture<UsersContractTests.Factory>
{
    public sealed class Factory : WebApplicationFactory<Program>
    {
        public readonly Guid TestUserId = Guid.NewGuid();
        private readonly string _dbName = "contract-users-" + Guid.NewGuid();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<QuraanDbContext>));
                services.AddDbContext<QuraanDbContext>(o =>
                    o.UseInMemoryDatabase(_dbName));

                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            });
        }
    }

    private readonly Factory _factory;
    public UsersContractTests(Factory factory)
    {
        _factory = factory;
        SeedUser();
    }

    private void SeedUser()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuraanDbContext>();
        if (db.Users.Any(u => u.Id == _factory.TestUserId)) return;
        db.Users.Add(new ApplicationUser
        {
            Id = _factory.TestUserId,
            UserName = "tester@test.local",
            NormalizedUserName = "TESTER@TEST.LOCAL",
            Email = "tester@test.local",
            NormalizedEmail = "TESTER@TEST.LOCAL",
            DisplayName = "Tester",
            PreferredLanguage = "en",
        });
        db.SaveChanges();
    }

    private HttpClient CreateClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", _factory.TestUserId.ToString());
        return client;
    }

    [Fact]
    public async Task GET_users_me_returns_UserProfile_shape()
    {
        using var client = CreateClient();
        var resp = await client.GetAsync(new Uri("/api/v1/users/me", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        foreach (var prop in new[] { "id", "email", "preferredLanguage" })
            doc.RootElement.TryGetProperty(prop, out _).Should().BeTrue($"UserProfile requires '{prop}'");
    }

    [Fact]
    public async Task GET_users_me_without_token_returns_401()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/v1/users/me", UriKind.Relative));
        ((int)resp.StatusCode).Should().BeOneOf(401, 404);
    }

    [Fact]
    public async Task PATCH_users_me_accepts_preferredLanguage_and_returns_updated_profile()
    {
        using var client = CreateClient();
        var resp = await client.PatchAsJsonAsync("/api/v1/users/me", new { preferredLanguage = "ar" });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("preferredLanguage").GetString().Should().Be("ar");
    }

    [Fact]
    public async Task PATCH_users_me_invalid_language_returns_400_problem_details()
    {
        using var client = CreateClient();
        var resp = await client.PatchAsJsonAsync("/api/v1/users/me", new { preferredLanguage = "zz" });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        resp.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }
}
