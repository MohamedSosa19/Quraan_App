namespace Quraan.Application.Auth;

public sealed record RegisterRequestDto(
    string Email,
    string Password,
    string? DisplayName,
    string? PreferredLanguage);
