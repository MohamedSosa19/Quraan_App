namespace Quraan.Application.Bookmarks;

/// <summary>
/// Thrown by <see cref="BookmarkService"/> when the per-user 1,000-active
/// bookmark cap (FR-030a) would be exceeded. The controller maps this to
/// a 409 ProblemDetails with the documented type URI.
/// </summary>
public sealed class BookmarkLimitReachedException : Exception
{
    public const string TypeUri = "https://quraan.app/problems/bookmark-limit-reached";

    public BookmarkLimitReachedException()
        : base("The 1,000-active-bookmarks-per-user cap has been reached.") { }
}

/// <summary>Thrown when an Ayah is already bookmarked by this user.</summary>
public sealed class BookmarkAlreadyExistsException : Exception
{
    public BookmarkAlreadyExistsException()
        : base("This Ayah is already bookmarked.") { }
}

/// <summary>Thrown when the requested Surah/Ayah pair does not resolve to a real Ayah.</summary>
public sealed class AyahNotFoundException : Exception
{
    public AyahNotFoundException()
        : base("No Ayah was found at the given Surah/Number coordinate.") { }
}
