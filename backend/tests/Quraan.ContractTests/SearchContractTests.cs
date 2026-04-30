using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quraan.Domain.Common;
using Quraan.Domain.Entities;
using Quraan.Infrastructure.Persistence;
using Xunit;

namespace Quraan.ContractTests;

public sealed class SearchContractTests : IClassFixture<SearchContractTests.Factory>
{
    public sealed class Factory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = "contract-search-" + Guid.NewGuid();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<QuraanDbContext>));
                services.AddDbContext<QuraanDbContext>(o =>
                    o.UseInMemoryDatabase(_dbName));
            });
        }
    }

    private readonly Factory _factory;
    public SearchContractTests(Factory factory)
    {
        _factory = factory;
        SeedMinimal();
    }

    private void SeedMinimal()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuraanDbContext>();
        if (db.Surahs.Any()) return;
        db.Translations.Add(new Translation
        {
            Id = 1, Code = "en.sahih", Language = "en", Name = "Saheeh", Attribution = "x",
        });
        db.Surahs.Add(new Surah
        {
            Id = 1, ArabicName = "الفاتحة", TransliteratedName = "Al-Fatiha",
            EnglishName = "The Opening", EnglishNameNormalized = "the opening",
            RevelationPlace = RevelationPlace.Meccan, AyahCount = 1,
        });
        var ayah = new Ayah
        {
            Id = 1, SurahId = 1, NumberInSurah = 1,
            ArabicText = "بسم الله", NormalizedArabicText = "بسم الله",
            JuzNumber = 1, HizbQuarter = 1, Sajda = false,
        };
        db.Ayahs.Add(ayah);
        db.AyahTranslations.Add(new AyahTranslation
        {
            AyahId = 1, TranslationId = 1,
            Text = "In the name of Allah",
            NormalizedText = "in the name of allah",
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task GET_search_returns_SearchResponse_shape()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/v1/search?q=name", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        foreach (var prop in new[] { "query", "surahMatches", "ayahMatches", "page", "pageSize", "totalAyahMatches" })
            doc.RootElement.TryGetProperty(prop, out _).Should().BeTrue($"SearchResponse requires '{prop}'");
    }

    [Fact]
    public async Task GET_search_AyahMatch_includes_matchedIn_and_highlightSnippet()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/v1/search?q=name", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var matches = doc.RootElement.GetProperty("ayahMatches");
        matches.GetArrayLength().Should().BeGreaterThan(0);

        var first = matches.EnumerateArray().First();
        foreach (var prop in new[] { "id", "surahId", "numberInSurah", "arabicText", "translationText", "matchedIn", "highlightSnippet" })
            first.TryGetProperty(prop, out _).Should().BeTrue($"AyahMatch requires '{prop}'");

        first.GetProperty("matchedIn").GetString().Should().BeOneOf("arabic", "translation");
    }

    [Fact]
    public async Task GET_search_empty_q_returns_400_problem_details()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/v1/search?q=", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        resp.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }
}
