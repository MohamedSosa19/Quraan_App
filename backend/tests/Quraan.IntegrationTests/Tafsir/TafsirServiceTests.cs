using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Quraan.Application.Tafsir;
using Quraan.Infrastructure.Persistence;
using Quraan.IntegrationTests.Fixtures;
using Xunit;

namespace Quraan.IntegrationTests.Tafsir;

/// <summary>
/// T132 — exercises GET /api/v1/tafsir/{surahId}/{numberInSurah} end-to-end
/// against the real SQL Server fixture. The TestDataSeeder seeds one Tafsir
/// entry for Al-Fatiha Ayah 2 so Ayahs 1, 3-7 can verify the "not available"
/// path required by FR-023.
/// </summary>
[Collection(nameof(SqlServerCollection))]
public sealed class TafsirServiceTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    public TafsirServiceTests(SqlServerFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuraanDbContext>();
        await TestDataSeeder.SeedAsync(db).ConfigureAwait(false);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Get_existing_tafsir_returns_body_source_and_attribution()
    {
        using var client = _fixture.Factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/v1/tafsir/1/2", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await resp.Content.ReadFromJsonAsync<TafsirEntryDto>();
        dto.Should().NotBeNull();
        dto!.SurahId.Should().Be(1);
        dto.NumberInSurah.Should().Be(2);
        dto.Body.Should().NotBeNullOrEmpty();
        dto.Source.Code.Should().Be("ibn-kathir-en");
        dto.Source.Name.Should().Contain("Ibn Kathir");
        dto.Source.Attribution.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Get_missing_tafsir_returns_404_problem_details_FR023()
    {
        using var client = _fixture.Factory.CreateClient();
        // Al-Fatiha Ayah 5 has no Tafsir entry seeded.
        var resp = await client.GetAsync(new Uri("/api/v1/tafsir/1/5", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        resp.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Get_unknown_source_returns_404()
    {
        using var client = _fixture.Factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/v1/tafsir/1/2?source=ar.unknown", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Out_of_range_surahId_is_rejected_by_route_constraint()
    {
        using var client = _fixture.Factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/v1/tafsir/200/1", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
