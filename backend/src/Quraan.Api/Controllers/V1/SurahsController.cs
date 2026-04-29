using Microsoft.AspNetCore.Mvc;
using Quraan.Application.Surahs;

namespace Quraan.Api.Controllers.V1;

[ApiController]
[Route("api/v1/surahs")]
public sealed class SurahsController : ControllerBase
{
    private readonly ISurahService _service;
    public SurahsController(ISurahService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SurahSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SurahSummaryDto>>> List(CancellationToken ct)
    {
        var list = await _service.GetAllAsync(ct).ConfigureAwait(false);
        return Ok(list);
    }

    [HttpGet("{surahId:int:min(1):max(114)}")]
    [ProducesResponseType(typeof(SurahDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SurahDetailDto>> Detail(
        int surahId,
        [FromQuery] string translation = "en.sahih",
        CancellationToken ct = default)
    {
        var detail = await _service.GetByIdAsync((byte)surahId, translation, ct).ConfigureAwait(false);
        if (detail is null) return Problem(statusCode: StatusCodes.Status404NotFound, title: "Surah not found", detail: $"No Surah with id {surahId} or unknown translation '{translation}'.");
        return Ok(detail);
    }
}
