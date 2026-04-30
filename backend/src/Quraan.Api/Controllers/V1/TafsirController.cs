using Microsoft.AspNetCore.Mvc;
using Quraan.Application.Tafsir;

namespace Quraan.Api.Controllers.V1;

[ApiController]
[Route("api/v1/tafsir")]
public sealed class TafsirController : ControllerBase
{
    private readonly ITafsirService _service;

    public TafsirController(ITafsirService service) => _service = service;

    [HttpGet("{surahId:int:min(1):max(114)}/{numberInSurah:int:min(1)}")]
    [ProducesResponseType(typeof(TafsirEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TafsirEntryDto>> Get(
        int surahId,
        int numberInSurah,
        [FromQuery] string source = "ibn-kathir-en",
        CancellationToken ct = default)
    {
        var result = await _service.GetForAyahAsync((byte)surahId, (short)numberInSurah, source, ct).ConfigureAwait(false);
        if (result is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Tafsir not available",
                detail: $"No Tafsir entry exists for Surah {surahId}, Ayah {numberInSurah} (source '{source}').");
        }
        return Ok(result);
    }
}
