namespace Quraan.Application.Bookmarks;

public sealed record BookmarkDto(
    Guid Id,
    int AyahId,
    byte SurahId,
    short NumberInSurah,
    DateTime CreatedAt);
