using Microsoft.AspNetCore.Mvc;
using Quraan.Application.Audio;
using Quraan.Domain.Repositories;

namespace Quraan.Api.Controllers.V1;

[ApiController]
[Route("api/v1/audio")]
public sealed class AudioController : ControllerBase
{
    private readonly IAudioService _service;
    public AudioController(IAudioService service) => _service = service;

    [HttpGet("{surahId:int:min(1):max(114)}")]
    [ProducesResponseType(typeof(AudioRecitationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<AudioRecitationDto>> Get(
        int surahId,
        [FromQuery] string reciter = "ar.alafasy",
        CancellationToken ct = default)
    {
        try
        {
            var result = await _service.GetRecitationAsync((byte)surahId, reciter, ct).ConfigureAwait(false);
            if (result is null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    title: "Audio not found",
                    detail: $"No reciter '{reciter}' or surah {surahId} unavailable.");
            return Ok(result);
        }
        catch (UpstreamAudioFailureException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Upstream audio source unreachable",
                detail: ex.Message);
        }
    }
}
