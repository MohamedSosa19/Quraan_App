using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Quraan.Application.Auth;
using Quraan.Infrastructure.Persistence;
using Quraan.IntegrationTests.Fixtures;
using Xunit;

namespace Quraan.IntegrationTests.Auth;

/// <summary>
/// T145 — FR-029b: 11th request within a 60-second window from the same IP
/// returns 429 ProblemDetails BEFORE any credential check. The
/// <see cref="System.Threading.RateLimiting.FixedWindowRateLimiterOptions"/>
/// limit is 10/minute/IP. Test sends 11 requests via the same TestServer
/// (which routes through 127.0.0.1) and expects the 11th to be 429.
/// </summary>
[Collection(nameof(SqlServerCollection))]
public sealed class IpRateLimitTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    public IpRateLimitTests(SqlServerFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuraanDbContext>();
        await TestDataSeeder.SeedAsync(db).ConfigureAwait(false);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Eleventh_login_attempt_within_60s_returns_429()
    {
        using var client = _fixture.Factory.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (int i = 0; i < 11; i++)
        {
            var resp = await client.PostAsJsonAsync(new Uri("/api/v1/auth/login", UriKind.Relative),
                new LoginRequestDto($"ratelimit-{i}@example.test", "anything"));
            statuses.Add(resp.StatusCode);
        }

        // First 10 hit the controller and return 401 (unknown email). The 11th
        // is rejected by the rate limiter before the controller runs.
        statuses.Take(10).Should().AllSatisfy(s => s.Should().Be(HttpStatusCode.Unauthorized));
        statuses[10].Should().Be(HttpStatusCode.TooManyRequests);
    }
}
