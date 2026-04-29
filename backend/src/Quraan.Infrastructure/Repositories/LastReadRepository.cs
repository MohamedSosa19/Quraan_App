using Microsoft.EntityFrameworkCore;
using Quraan.Domain.Entities;
using Quraan.Domain.Repositories;
using Quraan.Infrastructure.Persistence;

namespace Quraan.Infrastructure.Repositories;

public sealed class LastReadRepository : ILastReadRepository
{
    private readonly QuraanDbContext _db;
    public LastReadRepository(QuraanDbContext db) => _db = db;

    public Task<LastReadPosition?> GetAsync(Guid userId, CancellationToken ct = default) =>
        _db.LastReadPositions.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, ct);

    public async Task UpsertAsync(LastReadPosition position, CancellationToken ct = default)
    {
        var existing = await _db.LastReadPositions.FirstOrDefaultAsync(p => p.UserId == position.UserId, ct).ConfigureAwait(false);
        if (existing is null)
        {
            await _db.LastReadPositions.AddAsync(position, ct).ConfigureAwait(false);
        }
        else if (position.UpdatedAt > existing.UpdatedAt)
        {
            existing.SurahId = position.SurahId;
            existing.AyahNumberInSurah = position.AyahNumberInSurah;
            existing.UpdatedAt = position.UpdatedAt;
        }
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
