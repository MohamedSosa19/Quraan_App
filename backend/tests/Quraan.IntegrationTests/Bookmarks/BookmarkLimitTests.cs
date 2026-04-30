using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Quraan.Application.Bookmarks;
using Quraan.Domain.Entities;
using Quraan.Infrastructure.Identity;
using Quraan.Infrastructure.Persistence;
using Quraan.IntegrationTests.Fixtures;
using Quraan.IntegrationTests.Users;
using Xunit;

namespace Quraan.IntegrationTests.Bookmarks;

/// <summary>
/// T166 — FR-030a: hitting the 1,000-active-bookmarks-per-user cap returns
/// 409 with a distinct ProblemDetails type URI
/// (https://quraan.app/problems/bookmark-limit-reached) — different from
/// the generic "already-bookmarked" 409.
///
/// Implementation note: rather than truly bookmarking 1,000 ayahs (which the
/// test seed doesn't have), we directly write 1,000 active rows for the test
/// user, then issue ONE more POST and assert on the ProblemDetails type.
/// </summary>
[Collection(nameof(SqlServerCollection))]
public sealed class BookmarkLimitTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private readonly Guid _userId = Guid.NewGuid();
    public BookmarkLimitTests(SqlServerFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuraanDbContext>();
        await TestDataSeeder.SeedAsync(db).ConfigureAwait(false);

        if (!db.Users.Any(u => u.Id == _userId))
        {
            db.Users.Add(new ApplicationUser
            {
                Id = _userId,
                UserName = $"cap-{_userId:N}@test.local",
                NormalizedUserName = $"CAP-{_userId:N}@TEST.LOCAL",
                Email = $"cap-{_userId:N}@test.local",
                NormalizedEmail = $"CAP-{_userId:N}@TEST.LOCAL",
                PreferredLanguage = "en",
            });
        }

        // Backfill 1,000 active rows pointing at Al-Fatiha Ayah 1 — the unique
        // constraint is on (UserId, BookmarkId), not (UserId, AyahId), so this
        // is allowed. The cap check counts active rows, which is what we want
        // to exercise.
        var existing = db.Bookmarks.Count(b => b.UserId == _userId && !b.IsDeleted);
        for (int i = existing; i < BookmarkService.MaxActivePerUser; i++)
        {
            db.Bookmarks.Add(new Bookmark
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                AyahId = 1,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
            });
        }
        await db.SaveChangesAsync().ConfigureAwait(false);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Adding_above_the_cap_returns_409_with_bookmark_limit_reached_type()
    {
        var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Add(IntegrationTestAuthHandler.HeaderName, _userId.ToString());

        var resp = await client.PostAsJsonAsync(new Uri("/api/v1/bookmarks", UriKind.Relative),
            new CreateBookmarkRequestDto(1, 7));
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        resp.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain(BookmarkLimitReachedException.TypeUri);
    }
}
