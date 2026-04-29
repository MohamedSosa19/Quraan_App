using Microsoft.EntityFrameworkCore;
using Quraan.Domain.Entities;
using Quraan.Domain.Repositories;
using Quraan.Infrastructure.Persistence;

namespace Quraan.Infrastructure.Repositories;

public sealed class AyahRepository : IAyahRepository
{
    private readonly QuraanDbContext _db;
    public AyahRepository(QuraanDbContext db) => _db = db;

    public async Task<IReadOnlyList<Ayah>> GetBySurahAsync(byte surahId, byte translationId, CancellationToken ct = default)
    {
        return await _db.Ayahs.AsNoTracking()
            .Where(a => a.SurahId == surahId)
            .OrderBy(a => a.NumberInSurah)
            .Include(a => a.Translations.Where(t => t.TranslationId == translationId))
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public Task<Ayah?> GetByPositionAsync(byte surahId, short numberInSurah, byte translationId, CancellationToken ct = default) =>
        _db.Ayahs.AsNoTracking()
            .Where(a => a.SurahId == surahId && a.NumberInSurah == numberInSurah)
            .Include(a => a.Translations.Where(t => t.TranslationId == translationId))
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Ayah>> SearchArabicAsync(string normalizedQuery, int skip, int take, CancellationToken ct = default)
    {
        var pattern = $"%{normalizedQuery}%";
        return await _db.Ayahs.AsNoTracking()
            .Where(a => EF.Functions.Like(a.NormalizedArabicText, pattern))
            .OrderBy(a => a.SurahId).ThenBy(a => a.NumberInSurah)
            .Skip(skip).Take(take)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Ayah>> SearchTranslationAsync(string normalizedQuery, byte translationId, int skip, int take, CancellationToken ct = default)
    {
        var pattern = $"%{normalizedQuery}%";
        return await _db.AyahTranslations.AsNoTracking()
            .Where(t => t.TranslationId == translationId && EF.Functions.Like(t.NormalizedText, pattern))
            .Select(t => t.Ayah!)
            .OrderBy(a => a.SurahId).ThenBy(a => a.NumberInSurah)
            .Skip(skip).Take(take)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public Task<int> CountSearchAsync(string normalizedQuery, byte translationId, CancellationToken ct = default)
    {
        var pattern = $"%{normalizedQuery}%";
        return _db.AyahTranslations.AsNoTracking()
            .Where(t => t.TranslationId == translationId && EF.Functions.Like(t.NormalizedText, pattern))
            .CountAsync(ct);
    }

    public Task<int> CountArabicSearchAsync(string normalizedQuery, CancellationToken ct = default)
    {
        var pattern = $"%{normalizedQuery}%";
        return _db.Ayahs.AsNoTracking()
            .Where(a => EF.Functions.Like(a.NormalizedArabicText, pattern))
            .CountAsync(ct);
    }
}
