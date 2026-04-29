using Quraan.Domain.Entities;

namespace Quraan.Domain.Repositories;

public interface IAyahRepository
{
    Task<IReadOnlyList<Ayah>> GetBySurahAsync(byte surahId, byte translationId, CancellationToken ct = default);
    Task<Ayah?> GetByPositionAsync(byte surahId, short numberInSurah, byte translationId, CancellationToken ct = default);
    Task<IReadOnlyList<Ayah>> SearchArabicAsync(string normalizedQuery, int skip, int take, CancellationToken ct = default);
    Task<IReadOnlyList<Ayah>> SearchTranslationAsync(string normalizedQuery, byte translationId, int skip, int take, CancellationToken ct = default);
    Task<int> CountSearchAsync(string normalizedQuery, byte translationId, CancellationToken ct = default);
}
