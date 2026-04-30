using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Quraan.Application.LastRead;
using Quraan.Infrastructure.Identity;
using Quraan.Infrastructure.Persistence;
using Quraan.IntegrationTests.Fixtures;
using Quraan.IntegrationTests.Users;
using Xunit;

namespace Quraan.IntegrationTests.LastRead;

/// <summary>
/// T178 — Last-write-wins merge for /lastread/me, exercised against real
/// SQL Server. Simulates the anonymous-on-sign-in flow: client uploads a
/// localStorage value with `updatedAt`; if newer than the server's value
/// it wins, otherwise the server's value is preserved.
/// </summary>
[Collection(nameof(SqlServerCollection))]
public sealed class LastReadTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    public LastReadTests(SqlServerFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuraanDbContext>();
        await TestDataSeeder.SeedAsync(db).ConfigureAwait(false);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(HttpClient Client, Guid UserId)> AuthedClientAsync()
    {
        var userId = Guid.NewGuid();
        using (var scope = _fixture.Factory.Services.CreateScope())
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
            await db.SaveChangesAsync().ConfigureAwait(false);
        }
        var c = _fixture.Factory.CreateClient();
        c.DefaultRequestHeaders.Add(IntegrationTestAuthHandler.HeaderName, userId.ToString());
        return (c, userId);
    }

    [Fact]
    public async Task GET_when_unset_returns_200_null()
    {
        var (client, _) = await AuthedClientAsync();
        var resp = await client.GetAsync(new Uri("/api/v1/lastread/me", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        body.Trim().Should().Be("null");
    }

    [Fact]
    public async Task PUT_then_GET_round_trips()
    {
        var (client, _) = await AuthedClientAsync();
        var ts = DateTime.UtcNow;
        await client.PutAsJsonAsync(new Uri("/api/v1/lastread/me", UriKind.Relative),
            new LastReadPositionDto(2, 50, ts));

        var got = await client.GetFromJsonAsync<LastReadPositionDto>(new Uri("/api/v1/lastread/me", UriKind.Relative));
        got!.SurahId.Should().Be(2);
        got.NumberInSurah.Should().Be(50);
    }

    [Fact]
    public async Task PUT_with_older_updatedAt_does_not_overwrite_FR035()
    {
        var (client, _) = await AuthedClientAsync();
        var newer = DateTime.UtcNow;
        var older = newer.AddMinutes(-10);

        // Seed the server with the newer value.
        var first = await client.PutAsJsonAsync(new Uri("/api/v1/lastread/me", UriKind.Relative),
            new LastReadPositionDto(36, 1, newer));
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // Anonymous-on-sign-in merge — client uploads a stale localStorage
        // entry. Server keeps the newer value.
        var second = await client.PutAsJsonAsync(new Uri("/api/v1/lastread/me", UriKind.Relative),
            new LastReadPositionDto(1, 1, older));
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var got = await client.GetFromJsonAsync<LastReadPositionDto>(new Uri("/api/v1/lastread/me", UriKind.Relative));
        got!.SurahId.Should().Be(36);
        got.NumberInSurah.Should().Be(1);
    }

    [Fact]
    public async Task PUT_with_newer_updatedAt_overwrites()
    {
        var (client, _) = await AuthedClientAsync();
        var older = DateTime.UtcNow.AddMinutes(-10);
        var newer = DateTime.UtcNow;

        await client.PutAsJsonAsync(new Uri("/api/v1/lastread/me", UriKind.Relative),
            new LastReadPositionDto(1, 1, older));
        await client.PutAsJsonAsync(new Uri("/api/v1/lastread/me", UriKind.Relative),
            new LastReadPositionDto(36, 5, newer));

        var got = await client.GetFromJsonAsync<LastReadPositionDto>(new Uri("/api/v1/lastread/me", UriKind.Relative));
        got!.SurahId.Should().Be(36);
        got.NumberInSurah.Should().Be(5);
    }
}
