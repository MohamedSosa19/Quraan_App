using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Quraan.Application.Users;
using Quraan.Infrastructure.Identity;
using Quraan.Infrastructure.Persistence;
using Quraan.IntegrationTests.Fixtures;
using Xunit;

namespace Quraan.IntegrationTests.Users;

/// <summary>
/// T095 — PATCH /api/v1/users/me persists preferredLanguage; subsequent GET reflects it.
/// Uses the real SQL Server fixture + a header-based test auth handler so we can
/// hit the [Authorize] endpoint without spinning up the real JWT flow (which is US6 work).
/// </summary>
[Collection(nameof(SqlServerCollection))]
public sealed class UserProfileTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private readonly Guid _userId = Guid.NewGuid();

    public UserProfileTests(SqlServerFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuraanDbContext>();
        if (!db.Users.Any(u => u.Id == _userId))
        {
            db.Users.Add(new ApplicationUser
            {
                Id = _userId,
                UserName = "tester@quraan.local",
                NormalizedUserName = "TESTER@QURAAN.LOCAL",
                Email = "tester@quraan.local",
                NormalizedEmail = "TESTER@QURAAN.LOCAL",
                DisplayName = "Tester",
                PreferredLanguage = "en",
            });
            await db.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient CreateAuthedClient()
    {
        var factory = _fixture.Factory.WithWebHostBuilder(b =>
        {
            b.ConfigureServices(services =>
            {
                services.AddAuthentication(IntegrationTestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, IntegrationTestAuthHandler>(
                        IntegrationTestAuthHandler.SchemeName, _ => { });
            });
        });
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", _userId.ToString());
        return client;
    }

    [Fact]
    public async Task Patch_preferredLanguage_persists_and_survives_subsequent_GET()
    {
        using var client = CreateAuthedClient();

        var patch = await client.PatchAsJsonAsync("/api/v1/users/me", new UserProfilePatchDto(null, "ar"));
        patch.StatusCode.Should().Be(HttpStatusCode.OK);
        var patched = await patch.Content.ReadFromJsonAsync<UserProfileDto>();
        patched!.PreferredLanguage.Should().Be("ar");

        var get = await client.GetAsync(new Uri("/api/v1/users/me", UriKind.Relative));
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await get.Content.ReadFromJsonAsync<UserProfileDto>();
        profile!.PreferredLanguage.Should().Be("ar");
        profile.Email.Should().Be("tester@quraan.local");
    }
}
