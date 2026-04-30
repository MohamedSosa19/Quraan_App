namespace Quraan.Application.Bookmarks;

public interface IBookmarkService
{
    Task<BookmarkPageDto> ListAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<BookmarkDto> AddAsync(Guid userId, CreateBookmarkRequestDto req, CancellationToken ct = default);
    Task<bool> RemoveAsync(Guid userId, Guid bookmarkId, CancellationToken ct = default);
}
