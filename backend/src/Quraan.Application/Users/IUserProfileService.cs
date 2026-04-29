namespace Quraan.Application.Users;

public interface IUserProfileService
{
    Task<UserProfileDto?> GetMeAsync(Guid userId, CancellationToken ct = default);
    Task<UserProfileDto?> PatchMeAsync(Guid userId, UserProfilePatchDto patch, CancellationToken ct = default);
}
