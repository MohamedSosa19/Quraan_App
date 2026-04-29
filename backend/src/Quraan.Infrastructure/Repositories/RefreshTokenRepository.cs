using Microsoft.EntityFrameworkCore;
using Quraan.Domain.Entities;
using Quraan.Domain.Repositories;
using Quraan.Infrastructure.Persistence;

namespace Quraan.Infrastructure.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly QuraanDbContext _db;
    public RefreshTokenRepository(QuraanDbContext db) => _db = db;

    public Task<RefreshToken?> FindByHashAsync(byte[] tokenHash, CancellationToken ct = default) =>
        _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        await _db.RefreshTokens.AddAsync(token, ct).ConfigureAwait(false);
    }

    public Task UpdateAsync(RefreshToken token, CancellationToken ct = default)
    {
        _db.RefreshTokens.Update(token);
        return Task.CompletedTask;
    }

    public async Task RevokeChainAsync(Guid userId, CancellationToken ct = default)
    {
        var active = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(ct).ConfigureAwait(false);
        var now = DateTime.UtcNow;
        foreach (var t in active)
        {
            t.RevokedAt = now;
        }
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
