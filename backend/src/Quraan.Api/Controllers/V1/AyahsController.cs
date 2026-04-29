using Microsoft.AspNetCore.Mvc;
using Quraan.Application.Ayahs;
using Quraan.Application.Surahs;

namespace Quraan.Api.Controllers.V1;

[ApiController]
[Route("api/v1/surahs/{surahId:int:min(1):max(114)}/ayahs")]
public sealed class AyahsController : ControllerBase
{
    private readonly IAyahService _service;
    public AyahsController(IAyahService service) => _service = service;

    [HttpGet("{numberInSurah:int:min(1)}")]
    [ProducesResponseType(typeof(AyahDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AyahDto>> Get(
        int surahId,
        int numberInSurah,
        [FromQuery] string translation = "en.sahih",
        CancellationToken ct = default)
    {
        var ayah = await _service.GetAsync((byte)surahId, (short)numberInSurah, translation, ct).ConfigureAwait(false);
        if (ayah is null) return Problem(statusCode: StatusCodes.Status404NotFound, title: "Ayah not found", detail: $"No Ayah {numberInSurah} in Surah {surahId}, or unknown translation '{translation}'.");
        return Ok(ayah);
    }
}
