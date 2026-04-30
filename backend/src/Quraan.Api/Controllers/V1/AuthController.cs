using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Quraan.Application.Auth;

namespace Quraan.Api.Controllers.V1;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    public const string RefreshCookieName = "quraan-refresh";
    public const string RefreshCookiePath = "/api/v1/auth";

    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    [EnableRateLimiting("auth-ip")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto req, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _auth.RegisterAsync(req, ip, ct).ConfigureAwait(false);
        return result.Outcome switch
        {
            AuthOutcome.Ok => Created(result),
            AuthOutcome.EmailAlreadyInUse => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Email already in use",
                detail: "An account with this email already exists."),
            _ => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid registration",
                detail: "Registration could not be completed."),
        };
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth-ip")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto req, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _auth.LoginAsync(req, ip, ct).ConfigureAwait(false);
        return result.Outcome == AuthOutcome.Ok
            ? Ok(result)
            : GenericInvalidCredentials();
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AccessTokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var cookie = Request.Cookies[RefreshCookieName];
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _auth.RefreshAsync(cookie ?? string.Empty, ip, ct).ConfigureAwait(false);
        if (result.Outcome != AuthOutcome.Ok || result.Response is null)
        {
            ClearRefreshCookie();
            return Problem(statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid refresh token",
                detail: "Please sign in again.");
        }
        SetRefreshCookie(result.RefreshTokenCleartext!, result.RefreshTokenExpiresInSeconds!.Value);
        return Ok(new AccessTokenResponseDto(result.Response.AccessToken, result.Response.ExpiresInSeconds));
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var cookie = Request.Cookies[RefreshCookieName];
        await _auth.LogoutAsync(cookie, ct).ConfigureAwait(false);
        ClearRefreshCookie();
        return NoContent();
    }

    private ObjectResult Created(AuthResult result)
    {
        SetRefreshCookie(result.RefreshTokenCleartext!, result.RefreshTokenExpiresInSeconds!.Value);
        return StatusCode(StatusCodes.Status201Created, result.Response);
    }

    private OkObjectResult Ok(AuthResult result)
    {
        SetRefreshCookie(result.RefreshTokenCleartext!, result.RefreshTokenExpiresInSeconds!.Value);
        return Ok(result.Response);
    }

    private ObjectResult GenericInvalidCredentials()
    {
        // FR-029: identical generic response — no enumeration of cause.
        return Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Invalid credentials",
            detail: "The email or password you entered is incorrect.");
    }

    private void SetRefreshCookie(string cleartext, int maxAgeSeconds)
    {
        Response.Cookies.Append(RefreshCookieName, cleartext, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath,
            MaxAge = TimeSpan.FromSeconds(maxAgeSeconds),
            IsEssential = true,
        });
    }

    private void ClearRefreshCookie()
    {
        Response.Cookies.Append(RefreshCookieName, string.Empty, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath,
            Expires = DateTimeOffset.UnixEpoch,
        });
    }
}
