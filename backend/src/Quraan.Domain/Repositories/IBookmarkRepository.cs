using Quraan.Domain.Entities;

namespace Quraan.Domain.Repositories;

public interface IBookmarkRepository
{
    Task<IReadOnlyList<Bookmark>> ListAsync(Guid userId, int skip, int take, CancellationToken ct = default);
    Task<int> CountAsync(Guid userId, CancellationToken ct = default);
    Task<Bookmark?> GetAsync(Guid userId, Guid bookmarkId, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid userId, int ayahId, CancellationToken ct = default);
    Task AddAsync(Bookmark bookmark, CancellationToken ct = default);
    Task SoftDeleteAsync(Bookmark bookmark, CancellationToken ct = default);
}
