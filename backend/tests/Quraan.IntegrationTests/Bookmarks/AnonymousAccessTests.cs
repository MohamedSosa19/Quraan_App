using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Quraan.Application.Bookmarks;
using Quraan.Infrastructure.Persistence;
using Quraan.IntegrationTests.Fixtures;
using Xunit;

namespace Quraan.IntegrationTests.Bookmarks;

/// <summary>
/// T167 — every /bookmarks endpoint must return 401 ProblemDetails when the
/// caller is unauthenticated. The anonymous user must NEVER see real data
/// nor receive a 200/204/4xx-other response.
/// </summary>
[Collection(nameof(SqlServerCollection))]
public sealed class AnonymousAccessTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    public AnonymousAccessTests(SqlServerFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuraanDbContext>();
        await TestDataSeeder.SeedAsync(db).ConfigureAwait(false);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GET_bookmarks_anonymous_returns_401()
    {
        using var client = _fixture.Factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/v1/bookmarks", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_bookmarks_anonymous_returns_401()
    {
        using var client = _fixture.Factory.CreateClient();
        var resp = await client.PostAsJsonAsync(new Uri("/api/v1/bookmarks", UriKind.Relative),
            new CreateBookmarkRequestDto(1, 1));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DELETE_bookmark_anonymous_returns_401()
    {
        using var client = _fixture.Factory.CreateClient();
        var resp = await client.DeleteAsync(new Uri($"/api/v1/bookmarks/{Guid.NewGuid()}", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
