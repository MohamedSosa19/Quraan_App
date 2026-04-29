using FluentAssertions;
using Moq;
using Quraan.Application.Audio;
using Quraan.Application.Caching;
using Quraan.Domain.Entities;
using Quraan.Domain.Repositories;
using Xunit;

namespace Quraan.UnitTests.Audio;

public sealed class AudioServiceTests
{
    private readonly Mock<IReciterRepository> _reciters = new();
    private readonly Mock<IAudioUrlBuilder> _url = new();
    private readonly Mock<IAudioTimingProvider> _timings = new();
    private readonly PassThroughCache _cache = new();
    private readonly AudioService _service;

    public AudioServiceTests()
    {
        _service = new AudioService(_reciters.Object, _url.Object, _timings.Object, _cache);
    }

    private static Reciter Alafasy() => new()
    {
        Id = 1, Code = "ar.alafasy", Name = "Mishary Alafasy", ArabicName = "مشاري راشد العفاسي",
        AlQuranCloudId = "ar.alafasy", QuranComId = 7, Attribution = "© Reciter",
    };

    [Fact]
    public async Task Returns_null_when_reciter_unknown()
    {
        _reciters.Setup(r => r.GetByCodeAsync("zzz", It.IsAny<CancellationToken>())).ReturnsAsync((Reciter?)null);
        var result = await _service.GetRecitationAsync(78, "zzz");
        result.Should().BeNull();
    }

    [Fact]
    public async Task Composes_url_via_url_builder_for_known_reciter()
    {
        _reciters.Setup(r => r.GetByCodeAsync("ar.alafasy", It.IsAny<CancellationToken>())).ReturnsAsync(Alafasy());
        _url.Setup(u => u.BuildSurahAudioUrl(It.IsAny<Reciter>(), (byte)78))
            .Returns("https://cdn.islamic.network/quran/audio-surah/128/ar.alafasy/78.mp3");
        _timings.Setup(t => t.GetTimingsAsync(7, (byte)78, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioTimingResult(false, Array.Empty<AudioAyahTiming>()));

        var result = await _service.GetRecitationAsync(78, "ar.alafasy");
        result.Should().NotBeNull();
        result!.AudioUrl.Should().Be("https://cdn.islamic.network/quran/audio-surah/128/ar.alafasy/78.mp3");
        result.Reciter.Code.Should().Be("ar.alafasy");
        result.SurahId.Should().Be(78);
    }

    [Fact]
    public async Task Returns_empty_timings_when_provider_says_no_data_FR013()
    {
        _reciters.Setup(r => r.GetByCodeAsync("ar.alafasy", It.IsAny<CancellationToken>())).ReturnsAsync(Alafasy());
        _url.Setup(u => u.BuildSurahAudioUrl(It.IsAny<Reciter>(), It.IsAny<byte>())).Returns("https://x");
        _timings.Setup(t => t.GetTimingsAsync(It.IsAny<int>(), It.IsAny<byte>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioTimingResult(false, Array.Empty<AudioAyahTiming>()));

        var result = await _service.GetRecitationAsync(78, "ar.alafasy");
        result!.HasTimings.Should().BeFalse();
        result.AyahTimings.Should().BeEmpty();
    }

    [Fact]
    public async Task Maps_timings_when_provider_returns_them()
    {
        _reciters.Setup(r => r.GetByCodeAsync("ar.alafasy", It.IsAny<CancellationToken>())).ReturnsAsync(Alafasy());
        _url.Setup(u => u.BuildSurahAudioUrl(It.IsAny<Reciter>(), It.IsAny<byte>())).Returns("https://x");
        _timings.Setup(t => t.GetTimingsAsync(7, (byte)78, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioTimingResult(true, new[]
            {
                new AudioAyahTiming(1, 0, 1500),
                new AudioAyahTiming(2, 1500, 3200),
            }));

        var result = await _service.GetRecitationAsync(78, "ar.alafasy");
        result!.HasTimings.Should().BeTrue();
        result.AyahTimings.Should().HaveCount(2);
        result.AyahTimings[1].FromMs.Should().Be(1500);
    }

    [Fact]
    public async Task Returns_null_for_out_of_range_surahId()
    {
        var result = await _service.GetRecitationAsync(115, "ar.alafasy");
        result.Should().BeNull();
        _reciters.Verify(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Propagates_UpstreamAudioFailureException_from_provider()
    {
        _reciters.Setup(r => r.GetByCodeAsync("ar.alafasy", It.IsAny<CancellationToken>())).ReturnsAsync(Alafasy());
        _url.Setup(u => u.BuildSurahAudioUrl(It.IsAny<Reciter>(), It.IsAny<byte>())).Returns("https://x");
        _timings.Setup(t => t.GetTimingsAsync(It.IsAny<int>(), It.IsAny<byte>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UpstreamAudioFailureException("upstream 503"));

        var act = async () => await _service.GetRecitationAsync(78, "ar.alafasy");
        await act.Should().ThrowAsync<UpstreamAudioFailureException>();
    }

    private sealed class PassThroughCache : ICachedReader
    {
        public Task<T> GetOrAddAsync<T>(string key, TimeSpan ttl, Func<CancellationToken, Task<T>> factory, CancellationToken ct = default)
            where T : class
            => factory(ct);
    }
}
