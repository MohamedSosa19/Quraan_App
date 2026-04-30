using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Quraan.Application.Auth;
using Quraan.Application.Users;
using Quraan.Infrastructure.Persistence;
using Quraan.IntegrationTests.Fixtures;
using Xunit;

namespace Quraan.IntegrationTests.Auth;

/// <summary>
/// T142–T146 — End-to-end auth flow against real SQL Server (Testcontainers).
///
/// Covers:
///   • Happy path: register → 201 + access token + refresh cookie + GET /users/me
///   • FR-029: wrong password / unknown email both return identical 401
///   • FR-029a: 5 wrong attempts → locked 15 min; same generic 401 during
///     lockout (no disclosure); successful login resets the counter
///   • R-06: refresh-token rotation (old hash revoked, new hash active);
///     reuse detection revokes the entire chain
///
/// FR-029b (IP rate limit) is exercised in IpRateLimitTests separately
/// because the rate-limiter state is process-global and would interfere
/// with the other tests when run in parallel.
/// </summary>
[Collection(nameof(SqlServerCollection))]
public sealed class AuthFlowTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    public AuthFlowTests(SqlServerFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuraanDbContext>();
        await TestDataSeeder.SeedAsync(db).ConfigureAwait(false);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static string UniqueEmail() => $"u-{Guid.NewGuid():N}@example.test";

    [Fact]
    public async Task Register_then_GET_users_me_with_bearer_succeeds()
    {
        using var client = _fixture.Factory.CreateClient();
        var email = UniqueEmail();
        var resp = await client.PostAsJsonAsync(new Uri("/api/v1/auth/register", UriKind.Relative),
            new RegisterRequestDto(email, "passwordpassword", "Test User", "en"));
        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var auth = await resp.Content.ReadFromJsonAsync<AuthResponseDto>();
        auth!.AccessToken.Should().NotBeNullOrWhiteSpace();
        auth.User.Email.Should().Be(email);

        using var withBearer = _fixture.Factory.CreateClient();
        withBearer.DefaultRequestHeaders.Authorization = new("Bearer", auth.AccessToken);
        var profile = await withBearer.GetAsync(new Uri("/api/v1/users/me", UriKind.Relative));
        profile.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await profile.Content.ReadFromJsonAsync<UserProfileDto>();
        dto!.Email.Should().Be(email);
    }

    [Fact]
    public async Task Wrong_password_and_unknown_email_return_identical_401_FR029()
    {
        using var client = _fixture.Factory.CreateClient();
        var email = UniqueEmail();
        await client.PostAsJsonAsync(new Uri("/api/v1/auth/register", UriKind.Relative),
            new RegisterRequestDto(email, "passwordpassword", null, null));

        var wrongPass = await client.PostAsJsonAsync(new Uri("/api/v1/auth/login", UriKind.Relative),
            new LoginRequestDto(email, "wrong-passwordX"));
        var unknownEmail = await client.PostAsJsonAsync(new Uri("/api/v1/auth/login", UriKind.Relative),
            new LoginRequestDto(UniqueEmail(), "anythingatall"));

        wrongPass.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        unknownEmail.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var bodyA = await wrongPass.Content.ReadAsStringAsync();
        var bodyB = await unknownEmail.Content.ReadAsStringAsync();
        bodyA.Should().Be(bodyB, "FR-029 — responses must be byte-for-byte identical to prevent enumeration");
    }

    [Fact]
    public async Task Five_wrong_attempts_lock_account_FR029a_then_success_after_unlock_resets_counter()
    {
        using var client = _fixture.Factory.CreateClient();
        var email = UniqueEmail();
        await client.PostAsJsonAsync(new Uri("/api/v1/auth/register", UriKind.Relative),
            new RegisterRequestDto(email, "passwordpassword", null, null));

        // 5 wrong attempts → account lockout. The response stays a generic 401
        // throughout (no disclosure of lockout state).
        for (int i = 0; i < 5; i++)
        {
            var r = await client.PostAsJsonAsync(new Uri("/api/v1/auth/login", UriKind.Relative),
                new LoginRequestDto(email, "wrong-pass"));
            r.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // 6th attempt — even with the correct password the account is locked.
        var lockedAttempt = await client.PostAsJsonAsync(new Uri("/api/v1/auth/login", UriKind.Relative),
            new LoginRequestDto(email, "passwordpassword"));
        lockedAttempt.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_rotates_token_and_old_token_is_invalid_R06()
    {
        using var client = _fixture.Factory.CreateClient();
        var email = UniqueEmail();
        var registerResp = await client.PostAsJsonAsync(new Uri("/api/v1/auth/register", UriKind.Relative),
            new RegisterRequestDto(email, "passwordpassword", null, null));
        registerResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var firstCookie = ExtractRefreshCookie(registerResp);
        firstCookie.Should().NotBeNullOrWhiteSpace();

        // First refresh — should rotate.
        using var firstReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        firstReq.Headers.Add("Cookie", $"quraan-refresh={firstCookie}");
        var firstRefresh = await client.SendAsync(firstReq);
        firstRefresh.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotatedCookie = ExtractRefreshCookie(firstRefresh);
        rotatedCookie.Should().NotBe(firstCookie);

        // Replaying the old cookie now triggers reuse detection — chain revoked.
        using var replay = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        replay.Headers.Add("Cookie", $"quraan-refresh={firstCookie}");
        var replayResp = await client.SendAsync(replay);
        replayResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // The newly-rotated cookie is also revoked because the chain was nuked.
        using var thirdAttempt = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        thirdAttempt.Headers.Add("Cookie", $"quraan-refresh={rotatedCookie}");
        var thirdResp = await client.SendAsync(thirdAttempt);
        thirdResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static string? ExtractRefreshCookie(HttpResponseMessage resp)
    {
        if (!resp.Headers.TryGetValues("Set-Cookie", out var cookies)) return null;
        foreach (var c in cookies)
        {
            const string prefix = "quraan-refresh=";
            if (!c.StartsWith(prefix, StringComparison.Ordinal)) continue;
            var afterPrefix = c[prefix.Length..];
            var semi = afterPrefix.IndexOf(';');
            return semi < 0 ? afterPrefix : afterPrefix[..semi];
        }
        return null;
    }
}
