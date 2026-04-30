using Quraan.Application.Caching;
using Quraan.Domain.Repositories;

namespace Quraan.Application.Tafsir;

public sealed class TafsirService : ITafsirService
{
    private readonly ITafsirRepository _tafsir;
    private readonly ICachedReader _cache;

    public TafsirService(ITafsirRepository tafsir, ICachedReader cache)
    {
        _tafsir = tafsir;
        _cache = cache;
    }

    public async Task<TafsirEntryDto?> GetForAyahAsync(byte surahId, short numberInSurah, string sourceCode, CancellationToken ct = default)
    {
        if (surahId is < 1 or > 114) return null;
        if (numberInSurah < 1) return null;
        if (string.IsNullOrWhiteSpace(sourceCode)) return null;

        var source = await _tafsir.GetSourceByCodeAsync(sourceCode, ct).ConfigureAwait(false);
        if (source is null) return null;

        var key = CacheKeyFactory.Tafsir(surahId, numberInSurah, sourceCode);
        var entry = await _tafsir.GetForAyahAsync(surahId, numberInSurah, source.Id, ct).ConfigureAwait(false);
        if (entry is null) return null;

        var dto = new TafsirEntryDto(
            surahId,
            numberInSurah,
            new TafsirSourceDto(source.Code, source.Name, source.Attribution),
            entry.Body);

        // Cache the materialized DTO so subsequent calls skip the EF query.
        return await _cache.GetOrAddAsync<TafsirEntryDto>(
            key,
            CachedReader.ContentTtl,
            _ => Task.FromResult(dto),
            ct).ConfigureAwait(false);
    }
}
