using System.Diagnostics;
using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Quraan.Infrastructure.Persistence;
using Quraan.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace Quraan.IntegrationTests.Performance;

/// <summary>
/// T182 — Content-read perf smoke (Principle VI / SC-001).
/// Asserts p95 ≤ 300 ms across 100 cached <c>GET /api/v1/surahs/1</c> calls.
///
/// Tagged <c>Category=Performance</c> so it's excluded from the default CI run.
/// To run locally: <c>dotnet test --filter "Category=Performance"</c>.
/// Requires Docker (Testcontainers SQL Server) and a warm distributed cache —
/// the first request seeds the cache; the asserted percentile is computed over
/// the remaining 99.
/// </summary>
[Collection(nameof(SqlServerCollection))]
[Trait("Category", "Performance")]
public sealed class ContentReadPerfTests : IAsyncLifetime
{
    private const int Iterations = 100;
    private const double P95BudgetMs = 300.0;

    private readonly SqlServerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public ContentReadPerfTests(SqlServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    public async Task InitializeAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuraanDbContext>();
        await TestDataSeeder.SeedAsync(db).ConfigureAwait(false);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GET_surah_1_p95_under_300ms_when_cache_is_warm()
    {
        using var client = _fixture.Factory.CreateClient();
        var uri = new Uri("/api/v1/surahs/1?translation=en.sahih", UriKind.Relative);

        // Warm-up: seeds the distributed cache so subsequent calls hit cache.
        var warmup = await client.GetAsync(uri).ConfigureAwait(false);
        warmup.StatusCode.Should().Be(HttpStatusCode.OK, "cache warm-up call must succeed");

        var samples = new List<double>(Iterations);
        for (int i = 0; i < Iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            var resp = await client.GetAsync(uri).ConfigureAwait(false);
            sw.Stop();
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            samples.Add(sw.Elapsed.TotalMilliseconds);
        }

        var p50 = Percentile(samples, 0.50);
        var p95 = Percentile(samples, 0.95);
        var p99 = Percentile(samples, 0.99);
        _output.WriteLine($"GET /surahs/1  n={Iterations}  p50={p50:F1}ms  p95={p95:F1}ms  p99={p99:F1}ms");

        p95.Should().BeLessThanOrEqualTo(P95BudgetMs,
            $"SC-001 budget is {P95BudgetMs} ms p95; observed {p95:F1} ms");
    }

    private static double Percentile(IList<double> values, double p)
    {
        var sorted = values.OrderBy(v => v).ToArray();
        var rank = p * (sorted.Length - 1);
        var lo = (int)Math.Floor(rank);
        var hi = (int)Math.Ceiling(rank);
        if (lo == hi) return sorted[lo];
        return sorted[lo] + (rank - lo) * (sorted[hi] - sorted[lo]);
    }
}
