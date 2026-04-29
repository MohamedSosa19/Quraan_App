using FluentAssertions;
using Quraan.Domain.Entities;
using Quraan.Infrastructure.ExternalAudio;
using Xunit;

namespace Quraan.UnitTests.Audio;

public sealed class AlQuranCloudClientTests
{
    [Fact]
    public void Builds_canonical_R04_url_for_alafasy()
    {
        var c = new AlQuranCloudClient();
        var url = c.BuildSurahAudioUrl(new Reciter
        {
            Id = 1, Code = "ar.alafasy", AlQuranCloudId = "ar.alafasy", QuranComId = 7,
            Name = "Alafasy", ArabicName = "العفاسي", Attribution = "©",
        }, 78);
        url.Should().Be("https://cdn.islamic.network/quran/audio-surah/128/ar.alafasy/78.mp3");
    }

    [Theory]
    [InlineData((byte)0)]
    [InlineData((byte)115)]
    public void Rejects_out_of_range_surahId(byte surahId)
    {
        var c = new AlQuranCloudClient();
        var act = () => c.BuildSurahAudioUrl(new Reciter
        {
            Id = 1, Code = "ar.alafasy", AlQuranCloudId = "ar.alafasy",
            Name = "x", ArabicName = "x", Attribution = "x",
        }, surahId);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Throws_when_reciter_has_no_AlQuranCloudId()
    {
        var c = new AlQuranCloudClient();
        var act = () => c.BuildSurahAudioUrl(new Reciter
        {
            Id = 9, Code = "x.unknown", AlQuranCloudId = string.Empty,
            Name = "x", ArabicName = "x", Attribution = "x",
        }, 1);
        act.Should().Throw<InvalidOperationException>();
    }
}
