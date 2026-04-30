using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Quraan.Application.Auth;

namespace Quraan.Infrastructure.Identity;

public sealed class TokenService : ITokenService
{
    private const int AccessLifetimeSeconds = 15 * 60;       // 15 min
    private const int RefreshLifetimeSeconds = 30 * 24 * 3600; // 30 days
    private const int RefreshTokenBytes = 32;                 // 256 bits

    private readonly string _issuer;
    private readonly string _audience;
    private readonly SymmetricSecurityKey _signingKey;
    private readonly JwtSecurityTokenHandler _handler = new();

    public TokenService(IConfiguration config)
    {
        _issuer = config["Jwt:Issuer"] ?? "quraan-api";
        _audience = config["Jwt:Audience"] ?? "quraan-app";
        var key = config["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(key))
        {
            key = "DEV_SECRET_REPLACE_IN_PRODUCTION_AT_LEAST_32_BYTES_LONG_!!";
        }
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    }

    public (string AccessToken, int ExpiresInSeconds) IssueAccessToken(IEnumerable<Claim> claims)
    {
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now,
            expires: now.AddSeconds(AccessLifetimeSeconds),
            signingCredentials: new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256));
        return (_handler.WriteToken(token), AccessLifetimeSeconds);
    }

    public (string Cleartext, byte[] Hash, DateTime ExpiresAt) IssueRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(RefreshTokenBytes);
        var cleartext = Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return (cleartext, HashRefreshToken(cleartext), DateTime.UtcNow.AddSeconds(RefreshLifetimeSeconds));
    }

    public byte[] HashRefreshToken(string cleartext)
        => SHA256.HashData(Encoding.UTF8.GetBytes(cleartext));
}
