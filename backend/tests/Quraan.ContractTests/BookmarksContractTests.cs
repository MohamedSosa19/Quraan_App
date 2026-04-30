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
using Quraan.Application.Bookmarks;
using Quraan.ContractTests.TestAuth;
using Quraan.Domain.Common;
using Quraan.Domain.Entities;
using Quraan.Infrastructure.Identity;
using Quraan.Infrastructure.Persistence;
using Xunit;

namespace Quraan.ContractTests;

/// <summary>
/// T164 — Wire-shape contract for /bookmarks. Uses an in-memory store with a
/// stub TestAuthHandler so [Authorize] passes when X-Test-User is set.
/// </summary>
public sealed class BookmarksContractTests : IClassFixture<BookmarksContractTests.Factory>
{
    public sealed class Factory : WebApplicationFactory<Program>
    {
        public readonly Guid TestUserId = Guid.NewGuid();
        private readonly string _dbName = "contract-bookmarks-" + Guid.NewGuid();

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
    public BookmarksContractTests(Factory factory)
    {
        _factory = factory;
        Seed();
    }

    private void Seed()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuraanDbContext>();
        if (db.Users.Any(u => u.Id == _factory.TestUserId)) return;

        db.Users.Add(new ApplicationUser
        {
            Id = _factory.TestUserId,
            UserName = "bm@test.local", NormalizedUserName = "BM@TEST.LOCAL",
            Email = "bm@test.local", NormalizedEmail = "BM@TEST.LOCAL",
            PreferredLanguage = "en",
        });
        db.Surahs.Add(new Surah
        {
            Id = 1, ArabicName = "الفاتحة", TransliteratedName = "Al-Fatihah",
            EnglishName = "The Opener", EnglishNameNormalized = "the opener",
            RevelationPlace = RevelationPlace.Meccan, AyahCount = 7, OrderInRevelation = 1,
        });
        // Seed three Ayahs so each test can use a distinct (surahId, numberInSurah)
        // pair — tests share the in-memory DB and the duplicate-test asserts on
        // 1:2 specifically, so the success test must NOT collide with it.
        for (short n = 1; n <= 3; n++)
        {
            db.Ayahs.Add(new Ayah
            {
                Id = n, SurahId = 1, NumberInSurah = n,
                ArabicText = "بسم الله", NormalizedArabicText = "بسم الله",
                JuzNumber = 1, HizbQuarter = 1, Sajda = false,
            });
        }
        db.SaveChanges();
    }

    private HttpClient AuthedClient()
    {
        var c = _factory.CreateClient();
        c.DefaultRequestHeaders.Add("X-Test-User", _factory.TestUserId.ToString());
        return c;
    }

    [Fact]
    public async Task POST_bookmarks_returns_201_and_Bookmark_shape()
    {
        using var client = AuthedClient();
        var resp = await client.PostAsJsonAsync(new Uri("/api/v1/bookmarks", UriKind.Relative),
            new CreateBookmarkRequestDto(1, 1));
        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        foreach (var prop in new[] { "id", "ayahId", "surahId", "numberInSurah", "createdAt" })
            doc.RootElement.TryGetProperty(prop, out _).Should().BeTrue($"Bookmark requires '{prop}'");
    }

    [Fact]
    public async Task POST_bookmarks_duplicate_returns_409_problem_details()
    {
        using var client = AuthedClient();
        await client.PostAsJsonAsync(new Uri("/api/v1/bookmarks", UriKind.Relative),
            new CreateBookmarkRequestDto(1, 2));
        var dup = await client.PostAsJsonAsync(new Uri("/api/v1/bookmarks", UriKind.Relative),
            new CreateBookmarkRequestDto(1, 2));
        dup.StatusCode.Should().Be(HttpStatusCode.Conflict);
        dup.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task GET_bookmarks_returns_BookmarkPage_shape()
    {
        using var client = AuthedClient();
        var resp = await client.GetAsync(new Uri("/api/v1/bookmarks", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        foreach (var prop in new[] { "items", "page", "pageSize", "total" })
            doc.RootElement.TryGetProperty(prop, out _).Should().BeTrue($"BookmarkPage requires '{prop}'");
    }

    [Fact]
    public async Task GET_bookmarks_without_token_returns_401()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/v1/bookmarks", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DELETE_unknown_bookmark_returns_404()
    {
        using var client = AuthedClient();
        var resp = await client.DeleteAsync(new Uri($"/api/v1/bookmarks/{Guid.NewGuid()}", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
