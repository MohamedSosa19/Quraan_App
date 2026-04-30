namespace Quraan.Application.Auth;

public sealed record AccessTokenResponseDto(string AccessToken, int ExpiresInSeconds);
