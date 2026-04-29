using Quraan.Application.Caching;
using Quraan.Domain.Entities;
using Quraan.Domain.Repositories;

namespace Quraan.Application.Surahs;

public sealed class SurahService : ISurahService
{
    private readonly ISurahRepository _surahs;
    private readonly IAyahRepository _ayahs;
    private readonly ITranslationRepository _translations;
    private readonly ICachedReader _cache;

    public SurahService(
        ISurahRepository surahs,
        IAyahRepository ayahs,
        ITranslationRepository translations,
        ICachedReader cache)
    {
        _surahs = surahs;
        _ayahs = ayahs;
        _translations = translations;
        _cache = cache;
    }

    public Task<IReadOnlyList<SurahSummaryDto>> GetAllAsync(CancellationToken ct = default) =>
        _cache.GetOrAddAsync<IReadOnlyList<SurahSummaryDto>>(
            CacheKeyFactory.Surahs(),
            CachedReader.ContentTtl,
            async ctx =>
            {
                var rows = await _surahs.GetAllAsync(ctx).ConfigureAwait(false);
                return rows.Select(MapSummary).ToList();
            },
            ct);

    public async Task<SurahDetailDto?> GetByIdAsync(byte surahId, string translationCode, CancellationToken ct = default)
    {
        var translation = await _translations.GetByCodeAsync(translationCode, ct).ConfigureAwait(false);
        if (translation is null) return null;

        var key = CacheKeyFactory.Surah(surahId, translation.Code);
        var cached = await _cache.GetOrAddAsync<SurahDetailEnvelope>(
            key,
            CachedReader.ContentTtl,
            async ctx =>
            {
                var surah = await _surahs.GetByIdAsync(surahId, ctx).ConfigureAwait(false);
                if (surah is null) return new SurahDetailEnvelope(null);

                var ayahs = await _ayahs.GetBySurahAsync(surahId, translation.Id, ctx).ConfigureAwait(false);
                var info = new TranslationInfoDto(
                    translation.Code,
                    translation.Language,
                    translation.Name,
                    translation.Attribution);

                var detail = new SurahDetailDto(
                    surah.Id,
                    surah.ArabicName,
                    surah.TransliteratedName,
                    surah.EnglishName,
                    surah.RevelationPlace.ToString(),
                    surah.AyahCount,
                    info,
                    ayahs.Select(a => MapAyah(a, translation.Id)).ToList());

                return new SurahDetailEnvelope(detail);
            },
            ct).ConfigureAwait(false);

        return cached.Detail;
    }

    private static SurahSummaryDto MapSummary(Surah s) =>
        new(s.Id, s.ArabicName, s.TransliteratedName, s.EnglishName, s.RevelationPlace.ToString(), s.AyahCount);

    internal static AyahDto MapAyah(Ayah a, byte translationId)
    {
        var text = a.Translations.FirstOrDefault(t => t.TranslationId == translationId)?.Text ?? string.Empty;
        return new AyahDto(a.Id, a.SurahId, a.NumberInSurah, a.ArabicText, text, a.JuzNumber, a.HizbQuarter, a.Sajda);
    }

    private sealed record SurahDetailEnvelope(SurahDetailDto? Detail);
}
