using Quraan.Application.Caching;
using Quraan.Application.Surahs;
using Quraan.Domain.Repositories;

namespace Quraan.Application.Ayahs;

public sealed class AyahService : IAyahService
{
    private readonly IAyahRepository _ayahs;
    private readonly ITranslationRepository _translations;
    private readonly ICachedReader _cache;

    public AyahService(IAyahRepository ayahs, ITranslationRepository translations, ICachedReader cache)
    {
        _ayahs = ayahs;
        _translations = translations;
        _cache = cache;
    }

    public async Task<AyahDto?> GetAsync(byte surahId, short numberInSurah, string translationCode, CancellationToken ct = default)
    {
        var translation = await _translations.GetByCodeAsync(translationCode, ct).ConfigureAwait(false);
        if (translation is null) return null;

        var key = CacheKeyFactory.Ayah(surahId, numberInSurah, translation.Code);
        var envelope = await _cache.GetOrAddAsync<AyahEnvelope>(
            key,
            CachedReader.ContentTtl,
            async ctx =>
            {
                var ayah = await _ayahs.GetByPositionAsync(surahId, numberInSurah, translation.Id, ctx).ConfigureAwait(false);
                return new AyahEnvelope(ayah is null ? null : SurahService.MapAyah(ayah, translation.Id));
            },
            ct).ConfigureAwait(false);

        return envelope.Ayah;
    }

    private sealed record AyahEnvelope(AyahDto? Ayah);
}
