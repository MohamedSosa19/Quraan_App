namespace Quraan.Domain.Repositories;

public interface IUserProfileRepository
{
    Task<UserProfileSnapshot?> GetByIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Update mutable profile fields. Returns false if the user does not exist.</summary>
    Task<bool> UpdateAsync(Guid userId, string? displayName, string? preferredLanguage, CancellationToken ct = default);
}

public sealed record UserProfileSnapshot(
    Guid Id,
    string Email,
    string? DisplayName,
    string PreferredLanguage);
