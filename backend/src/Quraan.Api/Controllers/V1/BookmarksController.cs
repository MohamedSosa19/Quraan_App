using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraan.Application.Bookmarks;

namespace Quraan.Api.Controllers.V1;

[ApiController]
[Authorize]
[Route("api/v1/bookmarks")]
public sealed class BookmarksController : ControllerBase
{
    private readonly IBookmarkService _service;
    public BookmarksController(IBookmarkService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(BookmarkPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BookmarkPageDto>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var result = await _service.ListAsync(userId.Value, page, pageSize, ct).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(BookmarkDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateBookmarkRequestDto req, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        try
        {
            var dto = await _service.AddAsync(userId.Value, req, ct).ConfigureAwait(false);
            return StatusCode(StatusCodes.Status201Created, dto);
        }
        catch (BookmarkLimitReachedException ex)
        {
            return Problem(
                type: BookmarkLimitReachedException.TypeUri,
                title: "Bookmark limit reached",
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict);
        }
        catch (BookmarkAlreadyExistsException)
        {
            return Problem(
                title: "Already bookmarked",
                detail: "This Ayah is already in your bookmarks.",
                statusCode: StatusCodes.Status409Conflict);
        }
        catch (AyahNotFoundException)
        {
            return Problem(
                title: "Ayah not found",
                detail: "No Ayah was found at the given Surah/Number coordinate.",
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpDelete("{bookmarkId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid bookmarkId, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var ok = await _service.RemoveAsync(userId.Value, bookmarkId, ct).ConfigureAwait(false);
        return ok ? NoContent() : NotFound();
    }

    private Guid? GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
