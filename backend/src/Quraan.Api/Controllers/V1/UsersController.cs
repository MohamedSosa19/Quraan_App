using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraan.Application.Users;

namespace Quraan.Api.Controllers.V1;

[ApiController]
[Authorize]
[Route("api/v1/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserProfileService _service;
    public UsersController(IUserProfileService service) => _service = service;

    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileDto>> GetMe(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized");

        var profile = await _service.GetMeAsync(userId, ct).ConfigureAwait(false);
        if (profile is null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized");
        return Ok(profile);
    }

    [HttpPatch("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileDto>> PatchMe([FromBody] UserProfilePatchDto patch, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized");

        try
        {
            var updated = await _service.PatchMeAsync(userId, patch, ct).ConfigureAwait(false);
            if (updated is null)
                return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized");
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid request", detail: ex.Message);
        }
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out userId);
    }
}
