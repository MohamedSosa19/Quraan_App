using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Quraan.Application.Surahs;
using Quraan.Infrastructure.Persistence;
using Quraan.IntegrationTests.Fixtures;
using Xunit;

namespace Quraan.IntegrationTests.Surahs;

/// <summary>T074 — `GET /api/v1/surahs/1?translation=en.sahih` returns 7 Ayahs bilingually.</summary>
[Collection(nameof(SqlServerCollection))]
public sealed class SurahDetailTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    public SurahDetailTests(SqlServerFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuraanDbContext>();
        await TestDataSeeder.SeedAsync(db).ConfigureAwait(false);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Al_Fatiha_returns_7_Ayahs_with_Arabic_and_translation()
    {
        using var client = _fixture.Factory.CreateClient();
        var response = await client.GetAsync(new Uri("/api/v1/surahs/1?translation=en.sahih", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await response.Content.ReadFromJsonAsync<SurahDetailDto>();
        detail.Should().NotBeNull();
        detail!.Id.Should().Be(1);
        detail.AyahCount.Should().Be(7);
        detail.Ayahs.Should().HaveCount(7);

        detail.Translation.Code.Should().Be("en.sahih");
        detail.Translation.Language.Should().Be("en");
        detail.Translation.Name.Should().NotBeNullOrEmpty();
        detail.Translation.Attribution.Should().NotBeNullOrEmpty();

        var first = detail.Ayahs[0];
        first.NumberInSurah.Should().Be(1);
        first.ArabicText.Should().Contain("بِسْمِ");
        first.TranslationText.Should().Contain("In the name of Allah");
    }
}
