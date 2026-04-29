using Quraan.Domain.Repositories;

namespace Quraan.Application.Users;

public sealed class UserProfileService : IUserProfileService
{
    private static readonly string[] s_allowedLanguages = ["ar", "en"];
    private readonly IUserProfileRepository _repo;

    public UserProfileService(IUserProfileRepository repo) => _repo = repo;

    public async Task<UserProfileDto?> GetMeAsync(Guid userId, CancellationToken ct = default)
    {
        var snapshot = await _repo.GetByIdAsync(userId, ct).ConfigureAwait(false);
        return snapshot is null ? null : Map(snapshot);
    }

    public async Task<UserProfileDto?> PatchMeAsync(Guid userId, UserProfilePatchDto patch, CancellationToken ct = default)
    {
        if (patch.PreferredLanguage is not null && !s_allowedLanguages.Contains(patch.PreferredLanguage))
        {
            throw new ArgumentException("preferredLanguage must be 'ar' or 'en'.", nameof(patch));
        }

        var ok = await _repo.UpdateAsync(userId, patch.DisplayName, patch.PreferredLanguage, ct).ConfigureAwait(false);
        if (!ok) return null;
        return await GetMeAsync(userId, ct).ConfigureAwait(false);
    }

    private static UserProfileDto Map(UserProfileSnapshot s) =>
        new(s.Id, s.Email, s.DisplayName, s.PreferredLanguage);
}
