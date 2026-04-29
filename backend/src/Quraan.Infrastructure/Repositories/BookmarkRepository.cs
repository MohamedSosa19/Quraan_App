using Microsoft.EntityFrameworkCore;
using Quraan.Domain.Entities;
using Quraan.Domain.Repositories;
using Quraan.Infrastructure.Persistence;

namespace Quraan.Infrastructure.Repositories;

public sealed class BookmarkRepository : IBookmarkRepository
{
    private readonly QuraanDbContext _db;
    public BookmarkRepository(QuraanDbContext db) => _db = db;

    public async Task<IReadOnlyList<Bookmark>> ListAsync(Guid userId, int skip, int take, CancellationToken ct = default) =>
        await _db.Bookmarks.AsNoTracking()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Skip(skip).Take(take)
            .Include(b => b.Ayah)
            .ToListAsync(ct).ConfigureAwait(false);

    public Task<int> CountAsync(Guid userId, CancellationToken ct = default) =>
        _db.Bookmarks.AsNoTracking().CountAsync(b => b.UserId == userId, ct);

    public Task<Bookmark?> GetAsync(Guid userId, Guid bookmarkId, CancellationToken ct = default) =>
        _db.Bookmarks.FirstOrDefaultAsync(b => b.UserId == userId && b.Id == bookmarkId, ct);

    public Task<bool> ExistsAsync(Guid userId, int ayahId, CancellationToken ct = default) =>
        _db.Bookmarks.AsNoTracking().AnyAsync(b => b.UserId == userId && b.AyahId == ayahId, ct);

    public async Task AddAsync(Bookmark bookmark, CancellationToken ct = default)
    {
        await _db.Bookmarks.AddAsync(bookmark, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SoftDeleteAsync(Bookmark bookmark, CancellationToken ct = default)
    {
        bookmark.IsDeleted = true;
        bookmark.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
