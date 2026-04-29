using Quraan.Application.Caching;
using Quraan.Domain.Repositories;

namespace Quraan.Application.Audio;

public sealed class AudioService : IAudioService
{
    private readonly IReciterRepository _reciters;
    private readonly IAudioUrlBuilder _urlBuilder;
    private readonly IAudioTimingProvider _timings;
    private readonly ICachedReader _cache;

    public AudioService(
        IReciterRepository reciters,
        IAudioUrlBuilder urlBuilder,
        IAudioTimingProvider timings,
        ICachedReader cache)
    {
        _reciters = reciters;
        _urlBuilder = urlBuilder;
        _timings = timings;
        _cache = cache;
    }

    public async Task<AudioRecitationDto?> GetRecitationAsync(byte surahId, string reciterCode, CancellationToken ct = default)
    {
        if (surahId is < 1 or > 114) return null;

        var reciter = await _reciters.GetByCodeAsync(reciterCode, ct).ConfigureAwait(false);
        if (reciter is null) return null;

        var url = _urlBuilder.BuildSurahAudioUrl(reciter, surahId);

        var cacheKey = CacheKeyFactory.Audio(reciter.Code, surahId);
        var envelope = await _cache.GetOrAddAsync<TimingEnvelope>(
            cacheKey,
            CachedReader.ContentTtl,
            async ctx =>
            {
                var result = await _timings.GetTimingsAsync(reciter.QuranComId, surahId, ctx).ConfigureAwait(false);
                var mapped = result.Timings
                    .Select(t => new AyahTimingDto(t.NumberInSurah, t.FromMs, t.ToMs))
                    .ToList();
                return new TimingEnvelope(result.HasTimings, mapped);
            },
            ct).ConfigureAwait(false);

        var info = new ReciterInfoDto(reciter.Code, reciter.Name, reciter.ArabicName, reciter.Attribution);
        return new AudioRecitationDto(surahId, info, url, envelope.HasTimings, envelope.Timings);
    }

    private sealed record TimingEnvelope(bool HasTimings, IReadOnlyList<AyahTimingDto> Timings);
}
