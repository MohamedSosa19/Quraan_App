namespace Quraan.Application.Surahs;

public interface ISurahService
{
    Task<IReadOnlyList<SurahSummaryDto>> GetAllAsync(CancellationToken ct = default);
    Task<SurahDetailDto?> GetByIdAsync(byte surahId, string translationCode, CancellationToken ct = default);
}
