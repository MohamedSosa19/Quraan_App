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
/// T165 — End-to-end CRUD against real SQL Server: happy path,
/// duplicate-409, and soft-delete.
/// </summary>
[Collection(nameof(SqlServerCollection))]
public sealed class BookmarksCrudTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private readonly Guid _userId = Guid.NewGuid();
    public BookmarksCrudTests(SqlServerFixture fixture) => _fixture = fixture;

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
                UserName = $"crud-{_userId:N}@test.local",
                NormalizedUserName = $"CRUD-{_userId:N}@TEST.LOCAL",
                Email = $"crud-{_userId:N}@test.local",
                NormalizedEmail = $"CRUD-{_userId:N}@TEST.LOCAL",
                PreferredLanguage = "en",
            });
            await db.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient AuthedClient()
    {
        var c = _fixture.Factory.CreateClient();
        c.DefaultRequestHeaders.Add(IntegrationTestAuthHandler.HeaderName, _userId.ToString());
        return c;
    }

    [Fact]
    public async Task Create_then_list_returns_the_bookmark()
    {
        using var client = AuthedClient();
        var create = await client.PostAsJsonAsync(new Uri("/api/v1/bookmarks", UriKind.Relative),
            new CreateBookmarkRequestDto(1, 1));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await create.Content.ReadFromJsonAsync<BookmarkDto>();
        dto!.AyahId.Should().Be(1);

        var list = await client.GetFromJsonAsync<BookmarkPageDto>(new Uri("/api/v1/bookmarks", UriKind.Relative));
        list!.Items.Should().Contain(b => b.Id == dto.Id);
    }

    [Fact]
    public async Task Duplicate_create_returns_409()
    {
        using var client = AuthedClient();
        await client.PostAsJsonAsync(new Uri("/api/v1/bookmarks", UriKind.Relative), new CreateBookmarkRequestDto(1, 3));
        var dup = await client.PostAsJsonAsync(new Uri("/api/v1/bookmarks", UriKind.Relative), new CreateBookmarkRequestDto(1, 3));
        dup.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_soft_flips_IsDeleted_and_removes_from_list()
    {
        using var client = AuthedClient();
        var create = await client.PostAsJsonAsync(new Uri("/api/v1/bookmarks", UriKind.Relative), new CreateBookmarkRequestDto(1, 4));
        var dto = await create.Content.ReadFromJsonAsync<BookmarkDto>();

        var del = await client.DeleteAsync(new Uri($"/api/v1/bookmarks/{dto!.Id}", UriKind.Relative));
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // List no longer contains it.
        var list = await client.GetFromJsonAsync<BookmarkPageDto>(new Uri("/api/v1/bookmarks", UriKind.Relative));
        list!.Items.Should().NotContain(b => b.Id == dto.Id);

        // The DB row is soft-deleted, not physically removed.
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuraanDbContext>();
        var raw = db.Bookmarks.FirstOrDefault(b => b.Id == dto.Id);
        raw.Should().NotBeNull();
        raw!.IsDeleted.Should().BeTrue();
        raw.DeletedAt.Should().NotBeNull();
    }
}
