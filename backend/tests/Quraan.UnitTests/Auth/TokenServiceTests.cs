using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Quraan.Infrastructure.Identity;
using Xunit;

namespace Quraan.UnitTests.Auth;

public sealed class TokenServiceTests
{
    private const string SigningKey = "0123456789ABCDEF0123456789ABCDEF0123456789AB";

    private static TokenService BuildService(string? key = null) =>
        new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "test-issuer",
            ["Jwt:Audience"] = "test-audience",
            ["Jwt:SigningKey"] = key ?? SigningKey,
        }).Build());

    [Fact]
    public void IssueAccessToken_emits_a_15_minute_jwt_with_the_supplied_claims()
    {
        var svc = BuildService();
        var userId = Guid.NewGuid();
        var (token, expiresIn) = svc.IssueAccessToken(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "alice@example.test"),
        });

        expiresIn.Should().Be(900);
        token.Should().NotBeNullOrWhiteSpace();

        var jwt = new JwtSecurityToken(token);
        jwt.Issuer.Should().Be("test-issuer");
        jwt.Audiences.Should().Contain("test-audience");
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId.ToString());
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "alice@example.test");
        (jwt.ValidTo - jwt.ValidFrom).Should().BeCloseTo(TimeSpan.FromMinutes(15), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void IssueAccessToken_signature_validates_with_the_same_key()
    {
        var svc = BuildService();
        var (token, _) = svc.IssueAccessToken(new[] { new Claim("sub", "abc") });

        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var principal = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = "test-issuer",
            ValidateAudience = true, ValidAudience = "test-audience",
            ValidateIssuerSigningKey = true, IssuerSigningKey = key,
            ValidateLifetime = true,
        }, out _);
        principal.Should().NotBeNull();
    }

    [Fact]
    public void IssueRefreshToken_emits_unique_256_bit_tokens_with_30_day_lifetime()
    {
        var svc = BuildService();
        var (cleartext1, hash1, expires1) = svc.IssueRefreshToken();
        var (cleartext2, hash2, _) = svc.IssueRefreshToken();

        cleartext1.Should().NotBe(cleartext2);
        hash1.Should().NotEqual(hash2);
        hash1.Length.Should().Be(32); // SHA-256
        (expires1 - DateTime.UtcNow).Should().BeCloseTo(TimeSpan.FromDays(30), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void HashRefreshToken_is_deterministic_for_the_same_cleartext()
    {
        var svc = BuildService();
        var (cleartext, hash, _) = svc.IssueRefreshToken();
        var rehash = svc.HashRefreshToken(cleartext);
        rehash.Should().Equal(hash);
    }
}
