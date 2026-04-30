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
/// T182a — Search-latency perf smoke (Principle VI / SC-002).
/// Asserts p95 ≤ 1,000 ms across 100 mixed <c>GET /api/v1/search</c> calls,
/// rotating through the query mix from spec.md §performance:
///   • English word         → "name" (matches the seeded Saheeh translation)
///   • Arabic with diacritics → "الرَّحْمَٰنِ"
///   • Arabic without diacritics → "الرحمن"
///   • Surah-name → "Yaseen"
///   • No-results → "qwertyuiop"
///
/// Each query is exercised in BOTH cache-cold (first hit) and cache-warm (re-run)
/// states so the measurement reflects realistic mixed-load behavior.
///
/// Tagged <c>Category=Performance</c> so it's excluded from the default CI run.
/// To run locally: <c>dotnet test --filter "Category=Performance"</c>.
/// </summary>
[Collection(nameof(SqlServerCollection))]
[Trait("Category", "Performance")]
public sealed class SearchLatencyTests : IAsyncLifetime
{
    private const int Iterations = 100;
    private const double P95BudgetMs = 1000.0;

    private static readonly string[] QueryMix =
    [
        "name",
        "الرَّحْمَٰنِ",
        "الرحمن",
        "Yaseen",
        "qwertyuiop",
    ];

    private readonly SqlServerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public SearchLatencyTests(SqlServerFixture fixture, ITestOutputHelper output)
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
    public async Task Mixed_search_workload_p95_under_1000ms()
    {
        using var client = _fixture.Factory.CreateClient();
        var samples = new List<double>(Iterations);

        for (int i = 0; i < Iterations; i++)
        {
            var q = QueryMix[i % QueryMix.Length];
            var uri = new Uri($"/api/v1/search?q={Uri.EscapeDataString(q)}", UriKind.Relative);

            var sw = Stopwatch.StartNew();
            var resp = await client.GetAsync(uri).ConfigureAwait(false);
            sw.Stop();
            resp.StatusCode.Should().Be(HttpStatusCode.OK,
                $"search query '{q}' (iter {i}) must succeed");
            samples.Add(sw.Elapsed.TotalMilliseconds);
        }

        var p50 = Percentile(samples, 0.50);
        var p95 = Percentile(samples, 0.95);
        var p99 = Percentile(samples, 0.99);
        _output.WriteLine($"GET /search  n={Iterations}  p50={p50:F1}ms  p95={p95:F1}ms  p99={p99:F1}ms");

        p95.Should().BeLessThanOrEqualTo(P95BudgetMs,
            $"SC-002 budget is {P95BudgetMs} ms p95; observed {p95:F1} ms");
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
