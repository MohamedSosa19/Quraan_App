using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Quraan.Infrastructure.Persistence;
using Quraan.IntegrationTests.Fixtures;
using Xunit;

namespace Quraan.IntegrationTests.Surahs;

/// <summary>T075 — `/api/v1/surahs/115` returns 404 ProblemDetails.</summary>
[Collection(nameof(SqlServerCollection))]
public sealed class SurahNotFoundTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    public SurahNotFoundTests(SqlServerFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuraanDbContext>();
        await TestDataSeeder.SeedAsync(db).ConfigureAwait(false);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Out_of_range_surah_returns_404()
    {
        using var client = _fixture.Factory.CreateClient();
        var response = await client.GetAsync(new Uri("/api/v1/surahs/115", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Unknown_translation_returns_404_problem_details()
    {
        using var client = _fixture.Factory.CreateClient();
        var response = await client.GetAsync(new Uri("/api/v1/surahs/1?translation=unknown.code", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }
}
