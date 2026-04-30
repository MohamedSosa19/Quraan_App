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

/// <summary>
/// T131 — Validates the wire shape of GET /api/v1/tafsir/{surahId}/{numberInSurah}
/// against the OpenAPI TafsirEntry schema. Uses an in-memory database with a
/// single seeded entry so the assertions are decoupled from real seed JSON.
/// </summary>
public sealed class TafsirContractTests : IClassFixture<TafsirContractTests.Factory>
{
    public sealed class Factory : WebApplicationFactory<Program>
    {
        // One DB name per Factory so all scopes (seed + request + verify)
        // share the same in-memory store. Defining it as an instance field
        // means the optionsAction delegate captures a stable string instead
        // of evaluating Guid.NewGuid() every time the delegate is invoked.
        private readonly string _dbName = "contract-tafsir-" + Guid.NewGuid();

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
    public TafsirContractTests(Factory factory)
    {
        _factory = factory;
        Seed();
    }

    private void Seed()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuraanDbContext>();
        if (db.TafsirSources.Any()) return;

        db.Surahs.Add(new Surah
        {
            Id = 1, ArabicName = "الفاتحة", TransliteratedName = "Al-Fatihah",
            EnglishName = "The Opener", EnglishNameNormalized = "the opener",
            RevelationPlace = RevelationPlace.Meccan, AyahCount = 7, OrderInRevelation = 1,
        });
        db.Ayahs.Add(new Ayah
        {
            Id = 2, SurahId = 1, NumberInSurah = 2,
            ArabicText = "الْحَمْدُ لِلَّهِ رَبِّ الْعَالَمِينَ",
            NormalizedArabicText = "الحمد لله رب العالمين",
            JuzNumber = 1, HizbQuarter = 1, Sajda = false,
        });
        db.TafsirSources.Add(new TafsirSource
        {
            Id = 1, Code = "ibn-kathir-en",
            Name = "Tafsir Ibn Kathir (Mubarakpuri abridged)",
            Language = "en",
            Attribution = "Public-domain English digest by Mawlana Safi-ur-Rahman Mubarakpuri.",
        });
        db.TafsirEntries.Add(new TafsirEntry
        {
            Id = 1, AyahId = 2, TafsirSourceId = 1,
            Body = "Praise belongs to Allah, the Lord of all worlds…",
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task GET_tafsir_returns_TafsirEntry_shape()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/v1/tafsir/1/2", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        foreach (var prop in new[] { "surahId", "numberInSurah", "source", "body" })
            doc.RootElement.TryGetProperty(prop, out _).Should().BeTrue($"TafsirEntry requires '{prop}'");

        var source = doc.RootElement.GetProperty("source");
        foreach (var prop in new[] { "code", "name", "attribution" })
            source.TryGetProperty(prop, out _).Should().BeTrue($"source requires '{prop}'");

        source.GetProperty("code").GetString().Should().Be("ibn-kathir-en");
        doc.RootElement.GetProperty("body").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GET_tafsir_missing_entry_returns_404_problem_details()
    {
        using var client = _factory.CreateClient();
        // Surah 1 Ayah 5 has no Tafsir entry seeded.
        var resp = await client.GetAsync(new Uri("/api/v1/tafsir/1/5", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        resp.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task GET_tafsir_unknown_source_returns_404()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/v1/tafsir/1/2?source=does.not.exist", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
