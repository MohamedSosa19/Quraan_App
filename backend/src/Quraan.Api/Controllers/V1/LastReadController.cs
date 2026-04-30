using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraan.Application.LastRead;

namespace Quraan.Api.Controllers.V1;

[ApiController]
[Authorize]
[Route("api/v1/lastread")]
public sealed class LastReadController : ControllerBase
{
    private readonly ILastReadService _service;
    public LastReadController(ILastReadService service) => _service = service;

    [HttpGet("me")]
    [ProducesResponseType(typeof(LastReadPositionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var pos = await _service.GetAsync(userId.Value, ct).ConfigureAwait(false);
        // Per OpenAPI: 200 with null when unset. ControllerBase.Ok(null) maps
        // to 204; JsonResult lets us return 200 with literal `null` body.
        return new JsonResult(pos) { StatusCode = StatusCodes.Status200OK };
    }

    [HttpPut("me")]
    [ProducesResponseType(typeof(LastReadPositionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Put([FromBody] LastReadPositionDto incoming, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        try
        {
            var result = await _service.UpsertAsync(userId.Value, incoming, ct).ConfigureAwait(false);
            return Ok(result);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid last-read position",
                detail: ex.Message);
        }
    }

    private Guid? GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
