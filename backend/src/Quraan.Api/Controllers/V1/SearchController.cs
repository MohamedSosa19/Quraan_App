using Microsoft.AspNetCore.Mvc;
using Quraan.Application.Search;

namespace Quraan.Api.Controllers.V1;

[ApiController]
[Route("api/v1/search")]
public sealed class SearchController : ControllerBase
{
    private readonly ISearchService _service;
    public SearchController(ISearchService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(SearchResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SearchResponseDto>> Get(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string translation = "en.sahih",
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid query",
                detail: "Query parameter 'q' is required.");
        }

        try
        {
            var result = await _service.SearchAsync(q, page, pageSize, translation, ct).ConfigureAwait(false);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid query",
                detail: ex.Message);
        }
    }
}
