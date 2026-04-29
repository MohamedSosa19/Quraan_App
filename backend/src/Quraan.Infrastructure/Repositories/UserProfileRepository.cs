using Microsoft.EntityFrameworkCore;
using Quraan.Domain.Repositories;
using Quraan.Infrastructure.Persistence;

namespace Quraan.Infrastructure.Repositories;

public sealed class UserProfileRepository : IUserProfileRepository
{
    private readonly QuraanDbContext _db;
    public UserProfileRepository(QuraanDbContext db) => _db = db;

    public async Task<UserProfileSnapshot?> GetByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct).ConfigureAwait(false);
        return user is null
            ? null
            : new UserProfileSnapshot(user.Id, user.Email ?? string.Empty, user.DisplayName, user.PreferredLanguage);
    }

    public async Task<bool> UpdateAsync(Guid userId, string? displayName, string? preferredLanguage, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct).ConfigureAwait(false);
        if (user is null) return false;

        if (displayName is not null) user.DisplayName = displayName;
        if (preferredLanguage is not null) user.PreferredLanguage = preferredLanguage;

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }
}
