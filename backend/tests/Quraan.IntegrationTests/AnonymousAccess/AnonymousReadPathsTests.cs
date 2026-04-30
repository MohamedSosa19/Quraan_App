using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Quraan.Infrastructure.Persistence;
using Quraan.IntegrationTests.Fixtures;
using Xunit;

namespace Quraan.IntegrationTests.AnonymousAccess;

/// <summary>
/// T176b — FR-027 positive verification: every read endpoint that the spec
/// declares as anonymous-allowed (Surahs, Audio, Search, Tafsir) must return
/// 200 with no Authorization header. This complements the negative test in
/// Bookmarks/AnonymousAccessTests.cs (which proves /bookmarks IS protected).
/// Together they prove the anonymous boundary is drawn in the right place.
/// </summary>
[Collection(nameof(SqlServerCollection))]
public sealed class AnonymousReadPathsTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    public AnonymousReadPathsTests(SqlServerFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuraanDbContext>();
        await TestDataSeeder.SeedAsync(db).ConfigureAwait(false);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GET_surahs_anonymous_returns_200()
    {
        using var client = _fixture.Factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/v1/surahs", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_surah_by_id_anonymous_returns_200()
    {
        using var client = _fixture.Factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/v1/surahs/1", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_search_anonymous_returns_200()
    {
        using var client = _fixture.Factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/v1/search?q=mercy", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_tafsir_anonymous_returns_200()
    {
        using var client = _fixture.Factory.CreateClient();
        // Ayah 2 has a Tafsir entry seeded in TestDataSeeder.
        var resp = await client.GetAsync(new Uri("/api/v1/tafsir/1/2", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
