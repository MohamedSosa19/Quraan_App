using Quraan.Domain.Entities;
using Quraan.Domain.Repositories;

namespace Quraan.Application.Bookmarks;

public sealed class BookmarkService : IBookmarkService
{
    public const int MaxActivePerUser = 1_000;
    public const int MaxPageSize = 100;

    private readonly IBookmarkRepository _bookmarks;
    private readonly IAyahRepository _ayahs;

    public BookmarkService(IBookmarkRepository bookmarks, IAyahRepository ayahs)
    {
        _bookmarks = bookmarks;
        _ayahs = ayahs;
    }

    public async Task<BookmarkPageDto> ListAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var skip = (page - 1) * pageSize;

        var entries = await _bookmarks.ListAsync(userId, skip, pageSize, ct).ConfigureAwait(false);
        var total = await _bookmarks.CountAsync(userId, ct).ConfigureAwait(false);

        var items = entries
            .Where(b => b.Ayah is not null)
            .Select(b => new BookmarkDto(b.Id, b.AyahId, b.Ayah!.SurahId, b.Ayah.NumberInSurah, b.CreatedAt))
            .ToList();
        return new BookmarkPageDto(items, page, pageSize, total);
    }

    public async Task<BookmarkDto> AddAsync(Guid userId, CreateBookmarkRequestDto req, CancellationToken ct = default)
    {
        if (req.SurahId is < 1 or > 114) throw new AyahNotFoundException();
        if (req.NumberInSurah < 1) throw new AyahNotFoundException();

        var ayahId = await _ayahs.GetIdByPositionAsync(req.SurahId, req.NumberInSurah, ct).ConfigureAwait(false);
        if (ayahId is null) throw new AyahNotFoundException();

        if (await _bookmarks.ExistsAsync(userId, ayahId.Value, ct).ConfigureAwait(false))
            throw new BookmarkAlreadyExistsException();

        var count = await _bookmarks.CountAsync(userId, ct).ConfigureAwait(false);
        if (count >= MaxActivePerUser)
            throw new BookmarkLimitReachedException();

        var bookmark = new Bookmark
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AyahId = ayahId.Value,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false,
        };
        await _bookmarks.AddAsync(bookmark, ct).ConfigureAwait(false);

        return new BookmarkDto(bookmark.Id, bookmark.AyahId, req.SurahId, req.NumberInSurah, bookmark.CreatedAt);
    }

    public async Task<bool> RemoveAsync(Guid userId, Guid bookmarkId, CancellationToken ct = default)
    {
        var bookmark = await _bookmarks.GetAsync(userId, bookmarkId, ct).ConfigureAwait(false);
        if (bookmark is null) return false;
        await _bookmarks.SoftDeleteAsync(bookmark, ct).ConfigureAwait(false);
        return true;
    }
}
