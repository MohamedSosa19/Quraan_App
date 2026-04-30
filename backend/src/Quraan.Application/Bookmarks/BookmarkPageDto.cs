namespace Quraan.Application.Bookmarks;

public sealed record BookmarkPageDto(
    IReadOnlyList<BookmarkDto> Items,
    int Page,
    int PageSize,
    int Total);
