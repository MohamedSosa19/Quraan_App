using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Quraan.Application.Surahs;
using Quraan.Infrastructure.Persistence;
using Quraan.IntegrationTests.Fixtures;
using Xunit;

namespace Quraan.IntegrationTests.Surahs;

/// <summary>T073 — `GET /api/v1/surahs` returns 114 entries with required fields.</summary>
[Collection(nameof(SqlServerCollection))]
public sealed class SurahsListTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    public SurahsListTests(SqlServerFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuraanDbContext>();
        await TestDataSeeder.SeedAsync(db).ConfigureAwait(false);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Returns_all_114_surahs_in_order()
    {
        using var client = _fixture.Factory.CreateClient();
        var response = await client.GetAsync(new Uri("/api/v1/surahs", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await response.Content.ReadFromJsonAsync<List<SurahSummaryDto>>();
        list.Should().NotBeNull().And.HaveCount(114);
        list![0].Id.Should().Be(1);
        list[113].Id.Should().Be(114);

        var first = list[0];
        first.ArabicName.Should().NotBeNullOrEmpty();
        first.TransliteratedName.Should().NotBeNullOrEmpty();
        first.EnglishName.Should().NotBeNullOrEmpty();
        first.RevelationPlace.Should().BeOneOf("Meccan", "Medinan");
        first.AyahCount.Should().BeGreaterThan(0);
    }
}
