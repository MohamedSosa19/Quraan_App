namespace Quraan.Application.Users;

public sealed record UserProfileDto(
    Guid Id,
    string Email,
    string? DisplayName,
    string PreferredLanguage);
