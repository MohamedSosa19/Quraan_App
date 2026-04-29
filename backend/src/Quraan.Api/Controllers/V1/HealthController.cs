using Microsoft.AspNetCore.Mvc;

namespace Quraan.Api.Controllers.V1;

[ApiController]
[Route("api/v1/health")]
public sealed class HealthController : ControllerBase
{
    /// <summary>Liveness probe — always 200 if the process can serve a request.</summary>
    [HttpGet("live")]
    public IActionResult Live() => Ok(new { status = "live" });

    /// <summary>Readiness probe — wired up to dependency health checks (DB + cache) in <c>Program.cs</c>.</summary>
    [HttpGet("ready")]
    public IActionResult Ready() => Ok(new { status = "ready" });
}
