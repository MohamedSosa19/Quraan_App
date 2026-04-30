using System.Net;
using System.Net.Http.Json;
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

/// <summary>
/// T072 — contract test for `/api/v1/surahs` and `/api/v1/surahs/{id}`. Asserts
/// the response shape matches the schemas in
/// `specs/001-quran-mvp/contracts/openapi.yaml` (SurahSummary, SurahDetail,
/// TranslationInfo, Ayah). Uses an in-memory SQLite-style DbContext through
/// EF Core's UseInMemoryDatabase exclusion: that violates Principle III for
/// integration tests, so this contract test ONLY asserts wire-shape and is
/// allowed to use a stubbed DB. Real-DB coverage lives in
/// `Quraan.IntegrationTests/Surahs/*`.
/// </summary>
public sealed class SurahsContractTests : IClassFixture<SurahsContractTests.StubFactory>
{
    public sealed class StubFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = "contract-surahs-" + Guid.NewGuid();

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

    private readonly StubFactory _factory;
    public SurahsContractTests(StubFactory factory)
    {
        _factory = factory;
        SeedMinimal();
    }

    private void SeedMinimal()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuraanDbContext>();
        if (db.Surahs.Any()) return;

        var translation = new Translation
        {
            Id = 1, Code = "en.sahih", Language = "en",
            Name = "Saheeh International", Attribution = "Saheeh International",
        };
        db.Translations.Add(translation);

        var surah = new Surah
        {
            Id = 1, ArabicName = "الفاتحة", TransliteratedName = "Al-Fatiha",
            EnglishName = "The Opening", EnglishNameNormalized = "the opening",
            RevelationPlace = RevelationPlace.Meccan, AyahCount = 1, OrderInRevelation = 5,
        };
        db.Surahs.Add(surah);

        var ayah = new Ayah
        {
            Id = 1, SurahId = 1, NumberInSurah = 1,
            ArabicText = "بِسْمِ اللَّهِ الرَّحْمَٰنِ الرَّحِيمِ",
            NormalizedArabicText = "بسم الله الرحمن الرحيم",
            JuzNumber = 1, HizbQuarter = 1, Sajda = false,
        };
        db.Ayahs.Add(ayah);
        db.AyahTranslations.Add(new AyahTranslation
        {
            AyahId = 1, TranslationId = 1,
            Text = "In the name of Allah, the Entirely Merciful, the Especially Merciful.",
            NormalizedText = "in the name of allah, the entirely merciful, the especially merciful.",
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task GET_surahs_returns_array_of_SurahSummary()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/v1/surahs", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        var first = doc.RootElement.EnumerateArray().First();
        foreach (var prop in new[] { "id", "arabicName", "transliteratedName", "englishName", "revelationPlace", "ayahCount" })
            first.TryGetProperty(prop, out _).Should().BeTrue($"SurahSummary requires '{prop}'");
    }

    [Fact]
    public async Task GET_surah_by_id_returns_SurahDetail_with_translation_and_ayahs()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/v1/surahs/1?translation=en.sahih", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        foreach (var prop in new[] { "id", "arabicName", "transliteratedName", "englishName", "revelationPlace", "ayahCount", "translation", "ayahs" })
            root.TryGetProperty(prop, out _).Should().BeTrue($"SurahDetail requires '{prop}'");

        var translation = root.GetProperty("translation");
        foreach (var prop in new[] { "code", "language", "name", "attribution" })
            translation.TryGetProperty(prop, out _).Should().BeTrue($"TranslationInfo requires '{prop}'");

        var ayah = root.GetProperty("ayahs").EnumerateArray().First();
        foreach (var prop in new[] { "id", "surahId", "numberInSurah", "arabicText", "translationText", "juzNumber", "hizbQuarter", "sajda" })
            ayah.TryGetProperty(prop, out _).Should().BeTrue($"Ayah requires '{prop}'");
    }

    [Fact]
    public async Task GET_unknown_surah_returns_problem_details_404()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/v1/surahs/115", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        // ASP.NET parameter constraint produces a 404 before our controller runs;
        // when the constraint passes (e.g., id=99 with no row) we go through Problem(...).
        var resp2 = await client.GetAsync(new Uri("/api/v1/surahs/99", UriKind.Relative));
        resp2.StatusCode.Should().Be(HttpStatusCode.NotFound);
        resp2.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }
}
