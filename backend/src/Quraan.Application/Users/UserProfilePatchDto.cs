namespace Quraan.Application.Users;

public sealed record UserProfilePatchDto(
    string? DisplayName,
    string? PreferredLanguage);
