namespace Quraan.Application.Search;

public interface ISearchService
{
    Task<SearchResponseDto> SearchAsync(string query, int page, int pageSize, string translationCode, CancellationToken ct = default);
}
