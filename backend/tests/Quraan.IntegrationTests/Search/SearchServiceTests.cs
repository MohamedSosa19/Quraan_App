using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Quraan.Application.Search;
using Quraan.Infrastructure.Persistence;
using Quraan.IntegrationTests.Fixtures;
using Xunit;

namespace Quraan.IntegrationTests.Search;

/// <summary>
/// T120 — exercises GET /api/v1/search end-to-end against the real SQL Server
/// fixture. The TestDataSeeder seeds 114 Surah headers + Al-Fatiha's 7 ayahs
/// with Saheeh International translations. Scenarios pulled from spec.md US4
/// acceptance criteria are mapped to queries that match the seeded content
/// (e.g. "Opening" instead of the production-Tanzil "mercy" since the test
/// dataset doesn't span Surah 7).
/// </summary>
[Collection(nameof(SqlServerCollection))]
public sealed class SearchServiceTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    public SearchServiceTests(SqlServerFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuraanDbContext>();
        await TestDataSeeder.SeedAsync(db).ConfigureAwait(false);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<SearchResponseDto> SearchAsync(string q, int page = 1, int pageSize = 20)
    {
        using var client = _fixture.Factory.CreateClient();
        var resp = await client.GetAsync(new Uri($"/api/v1/search?q={Uri.EscapeDataString(q)}&page={page}&pageSize={pageSize}", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await resp.Content.ReadFromJsonAsync<SearchResponseDto>())!;
    }

    [Fact]
    public async Task English_query_matches_translation_text_with_matchedIn_translation()
    {
        var result = await SearchAsync("name");
        result.AyahMatches.Should().NotBeEmpty();
        result.AyahMatches.Should().Contain(m => m.MatchedIn == "translation");
        result.AyahMatches[0].HighlightSnippet.Should().NotBeNullOrEmpty();
        result.TotalAyahMatches.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Surah_name_query_returns_Yaseen()
    {
        var result = await SearchAsync("Yaseen");
        result.SurahMatches.Should().Contain(s => s.Id == 36);
    }

    [Fact]
    public async Task Surah_name_query_returns_Opening_for_Al_Fatihah()
    {
        var result = await SearchAsync("Opening");
        result.SurahMatches.Should().Contain(s => s.Id == 1);
    }

    [Fact]
    public async Task Diacritic_insensitive_arabic_query_matches_diacritic_text_FR017()
    {
        // Seeded Al-Fatiha 1: "بِسْمِ اللَّهِ الرَّحْمَٰنِ الرَّحِيمِ"
        // Query without diacritics — normalizer strips both query and stored text.
        var result = await SearchAsync("الرحمن");
        result.AyahMatches.Should().NotBeEmpty();
        result.AyahMatches.Should().Contain(m => m.MatchedIn == "arabic" && m.SurahId == 1);
    }

    [Fact]
    public async Task No_match_query_returns_empty_arrays_with_zero_total()
    {
        var result = await SearchAsync("qwertyuiop");
        result.SurahMatches.Should().BeEmpty();
        result.AyahMatches.Should().BeEmpty();
        result.TotalAyahMatches.Should().Be(0);
    }

    [Fact]
    public async Task PageSize_query_param_is_capped_at_100()
    {
        var result = await SearchAsync("a", pageSize: 500);
        result.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task Empty_query_returns_400_problem_details()
    {
        using var client = _fixture.Factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/v1/search?q=", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        resp.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }
}
