using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Quraan.Application.Auth;
using Quraan.Application.Users;
using Quraan.Domain.Entities;
using Quraan.Domain.Repositories;

namespace Quraan.Infrastructure.Identity;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly ITokenService _tokens;
    private readonly IRefreshTokenRepository _refresh;

    public AuthService(
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn,
        ITokenService tokens,
        IRefreshTokenRepository refresh)
    {
        _users = users;
        _signIn = signIn;
        _tokens = tokens;
        _refresh = refresh;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequestDto req, string? createdByIp, CancellationToken ct = default)
    {
        var existing = await _users.FindByEmailAsync(req.Email).ConfigureAwait(false);
        if (existing is not null)
        {
            return new AuthResult(AuthOutcome.EmailAlreadyInUse);
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = req.Email,
            Email = req.Email,
            DisplayName = req.DisplayName,
            PreferredLanguage = string.IsNullOrWhiteSpace(req.PreferredLanguage) ? "en" : req.PreferredLanguage,
        };
        var result = await _users.CreateAsync(user, req.Password).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            // Treat any creation failure as invalid input — the FluentValidation
            // pipeline should already have caught format issues, so this is a
            // defense-in-depth fallback.
            return new AuthResult(AuthOutcome.ValidationFailed);
        }

        return await IssueTokensAsync(user, createdByIp, ct).ConfigureAwait(false);
    }

    public async Task<AuthResult> LoginAsync(LoginRequestDto req, string? createdByIp, CancellationToken ct = default)
    {
        var user = await _users.FindByEmailAsync(req.Email).ConfigureAwait(false);
        if (user is null)
        {
            // Don't disclose whether the email exists — return the same generic
            // 401 (FR-029). We still spend a comparable amount of time below
            // by using a dummy password check on a known-bad hash.
            await _signIn.PasswordSignInAsync(req.Email, req.Password, isPersistent: false, lockoutOnFailure: false)
                .ConfigureAwait(false);
            return new AuthResult(AuthOutcome.InvalidCredentials);
        }

        var sign = await _signIn.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: true).ConfigureAwait(false);
        if (!sign.Succeeded)
        {
            // Lockout, wrong password — all funnel into the same generic error
            // (FR-029a: lockout state is NOT disclosed).
            return new AuthResult(AuthOutcome.InvalidCredentials);
        }

        await _users.ResetAccessFailedCountAsync(user).ConfigureAwait(false);
        user.LastSignInAt = DateTime.UtcNow;
        await _users.UpdateAsync(user).ConfigureAwait(false);

        return await IssueTokensAsync(user, createdByIp, ct).ConfigureAwait(false);
    }

    public async Task<AuthResult> RefreshAsync(string refreshTokenCleartext, string? createdByIp, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenCleartext))
            return new AuthResult(AuthOutcome.RefreshInvalid);

        var hash = _tokens.HashRefreshToken(refreshTokenCleartext);
        var token = await _refresh.FindByHashAsync(hash, ct).ConfigureAwait(false);
        if (token is null)
            return new AuthResult(AuthOutcome.RefreshInvalid);

        var now = DateTime.UtcNow;
        if (!token.IsActive(now))
        {
            // Reuse detection (R-06): an already-revoked or expired token is
            // being presented — revoke the entire chain for this user.
            await _refresh.RevokeChainAsync(token.UserId, ct).ConfigureAwait(false);
            await _refresh.SaveChangesAsync(ct).ConfigureAwait(false);
            return new AuthResult(AuthOutcome.RefreshInvalid);
        }

        var user = await _users.FindByIdAsync(token.UserId.ToString()).ConfigureAwait(false);
        if (user is null)
            return new AuthResult(AuthOutcome.RefreshInvalid);

        // Rotate: revoke the old, issue + persist a new one.
        var (cleartext, newHash, expiresAt) = _tokens.IssueRefreshToken();
        token.RevokedAt = now;
        token.ReplacedByTokenHash = newHash;
        await _refresh.UpdateAsync(token, ct).ConfigureAwait(false);

        await _refresh.AddAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = newHash,
            IssuedAt = now,
            ExpiresAt = expiresAt,
            CreatedByIp = createdByIp,
        }, ct).ConfigureAwait(false);
        await _refresh.SaveChangesAsync(ct).ConfigureAwait(false);

        var (accessToken, expiresIn) = IssueAccessToken(user);
        var profile = new UserProfileDto(user.Id, user.Email ?? string.Empty, user.DisplayName, user.PreferredLanguage);
        var response = new AuthResponseDto(accessToken, expiresIn, profile);
        var refreshLifetime = (int)(expiresAt - now).TotalSeconds;
        return new AuthResult(AuthOutcome.Ok, response, cleartext, refreshLifetime);
    }

    public async Task LogoutAsync(string? refreshTokenCleartext, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenCleartext)) return;

        var hash = _tokens.HashRefreshToken(refreshTokenCleartext);
        var token = await _refresh.FindByHashAsync(hash, ct).ConfigureAwait(false);
        if (token is null) return;
        if (token.RevokedAt is null) token.RevokedAt = DateTime.UtcNow;
        await _refresh.UpdateAsync(token, ct).ConfigureAwait(false);
        await _refresh.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task<AuthResult> IssueTokensAsync(ApplicationUser user, string? createdByIp, CancellationToken ct)
    {
        var (accessToken, expiresIn) = IssueAccessToken(user);
        var (cleartext, hash, expiresAt) = _tokens.IssueRefreshToken();

        await _refresh.AddAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = hash,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            CreatedByIp = createdByIp,
        }, ct).ConfigureAwait(false);
        await _refresh.SaveChangesAsync(ct).ConfigureAwait(false);

        var profile = new UserProfileDto(user.Id, user.Email ?? string.Empty, user.DisplayName, user.PreferredLanguage);
        var response = new AuthResponseDto(accessToken, expiresIn, profile);
        var refreshLifetime = (int)(expiresAt - DateTime.UtcNow).TotalSeconds;
        return new AuthResult(AuthOutcome.Ok, response, cleartext, refreshLifetime);
    }

    private (string AccessToken, int ExpiresInSeconds) IssueAccessToken(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("sub", user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
        };
        if (!string.IsNullOrWhiteSpace(user.DisplayName))
            claims.Add(new Claim("name", user.DisplayName));
        return _tokens.IssueAccessToken(claims);
    }
}
