using System.Security.Claims;

namespace Quraan.Application.Auth;

public interface ITokenService
{
    /// <summary>
    /// Issues a JWT access token (15-minute lifetime, HS256). Returns the
    /// signed token string and its `exp` lifetime in seconds.
    /// </summary>
    (string AccessToken, int ExpiresInSeconds) IssueAccessToken(IEnumerable<Claim> claims);

    /// <summary>
    /// Generates a 256-bit cryptographically random opaque refresh token and
    /// its SHA-256 hash. Only the hash should be persisted (R-06).
    /// </summary>
    (string Cleartext, byte[] Hash, DateTime ExpiresAt) IssueRefreshToken();

    /// <summary>SHA-256 of the cleartext refresh-token string.</summary>
    byte[] HashRefreshToken(string cleartext);
}
