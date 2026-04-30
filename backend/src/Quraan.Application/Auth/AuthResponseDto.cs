using Quraan.Application.Users;

namespace Quraan.Application.Auth;

public sealed record AuthResponseDto(
    string AccessToken,
    int ExpiresInSeconds,
    UserProfileDto User);
