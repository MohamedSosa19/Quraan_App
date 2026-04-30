namespace Quraan.Application.Auth;

/// <summary>
/// Result of an auth operation. Returns the JWT + refresh-token cleartext when
/// successful so the controller can set the Set-Cookie header. Failures are
/// signaled by Outcome != Ok; the controller maps them to identical generic
/// problem details (FR-029) so callers cannot distinguish lockout from wrong
/// password from unknown email.
/// </summary>
public sealed record AuthResult(
    AuthOutcome Outcome,
    AuthResponseDto? Response = null,
    string? RefreshTokenCleartext = null,
    int? RefreshTokenExpiresInSeconds = null);

public enum AuthOutcome
{
    Ok,
    InvalidCredentials,    // wrong password, unknown email, locked, deleted — all map to one 401
    EmailAlreadyInUse,     // 409 (FR-024a)
    ValidationFailed,      // 400 (handled by FluentValidation pipeline before service)
    RefreshInvalid,        // 401 on refresh
}
