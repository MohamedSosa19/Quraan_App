namespace Quraan.Application.Auth;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequestDto req, string? createdByIp, CancellationToken ct = default);
    Task<AuthResult> LoginAsync(LoginRequestDto req, string? createdByIp, CancellationToken ct = default);
    Task<AuthResult> RefreshAsync(string refreshTokenCleartext, string? createdByIp, CancellationToken ct = default);
    Task LogoutAsync(string? refreshTokenCleartext, CancellationToken ct = default);
}
