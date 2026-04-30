using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quraan.Domain.Entities;
using Quraan.Domain.Repositories;
using Quraan.Infrastructure.Persistence;
using Xunit;

namespace Quraan.ContractTests;

public sealed class AudioContractTests : IClassFixture<AudioContractTests.Factory>
{
    public sealed class Factory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = "contract-audio-" + Guid.NewGuid();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<QuraanDbContext>));
                services.AddDbContext<QuraanDbContext>(o =>
                    o.UseInMemoryDatabase(_dbName));

                // Replace the live IAudioTimingProvider so the contract test
                // never hits quran.com over the network.
                services.RemoveAll(typeof(IAudioTimingProvider));
                services.AddSingleton<IAudioTimingProvider, StubTimingProvider>();
            });
        }
    }

    private readonly Factory _factory;
    public AudioContractTests(Factory factory)
    {
        _factory = factory;
        SeedReciter();
    }

    private void SeedReciter()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuraanDbContext>();
        if (db.Reciters.Any()) return;
        db.Reciters.Add(new Reciter
        {
            Id = 1, Code = "ar.alafasy", Name = "Alafasy", ArabicName = "العفاسي",
            AlQuranCloudId = "ar.alafasy", QuranComId = 7,
            Attribution = "Mishary Alafasy / Al Quran Cloud",
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task GET_audio_returns_AudioRecitation_shape()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/v1/audio/78", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        foreach (var prop in new[] { "surahId", "reciter", "audioUrl", "hasTimings", "ayahTimings" })
            doc.RootElement.TryGetProperty(prop, out _).Should().BeTrue($"AudioRecitation requires '{prop}'");

        var reciter = doc.RootElement.GetProperty("reciter");
        foreach (var prop in new[] { "code", "name", "arabicName", "attribution" })
            reciter.TryGetProperty(prop, out _).Should().BeTrue($"reciter requires '{prop}'");

        doc.RootElement.GetProperty("audioUrl").GetString()
            .Should().StartWith("https://cdn.islamic.network/quran/audio-surah/128/ar.alafasy/78");
    }

    [Fact]
    public async Task GET_audio_unknown_reciter_returns_404_problem_details()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/v1/audio/78?reciter=does.not.exist", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        resp.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task GET_audio_upstream_failure_returns_502_problem_details()
    {
        using var client = _factory.CreateClient();
        // Surah 99 triggers the stub to throw.
        var resp = await client.GetAsync(new Uri("/api/v1/audio/99", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        resp.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    /// <summary>Test stub: surah 99 throws upstream-failure; everything else returns empty timings.</summary>
    private sealed class StubTimingProvider : IAudioTimingProvider
    {
        public Task<AudioTimingResult> GetTimingsAsync(int recitationId, byte surahId, CancellationToken ct = default)
        {
            if (surahId == 99)
                throw new UpstreamAudioFailureException("stubbed upstream failure");
            return Task.FromResult(new AudioTimingResult(false, Array.Empty<AudioAyahTiming>()));
        }
    }
}
