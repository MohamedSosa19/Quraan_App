using FluentAssertions;
using Moq;
using Quraan.Application.Caching;
using Quraan.Application.Surahs;
using Quraan.Domain.Common;
using Quraan.Domain.Entities;
using Quraan.Domain.Repositories;
using Xunit;

namespace Quraan.UnitTests.Surahs;

public sealed class SurahServiceTests
{
    private readonly Mock<ISurahRepository> _surahs = new();
    private readonly Mock<IAyahRepository> _ayahs = new();
    private readonly Mock<ITranslationRepository> _translations = new();
    private readonly PassThroughCache _cache = new();
    private readonly SurahService _service;

    public SurahServiceTests()
    {
        _service = new SurahService(_surahs.Object, _ayahs.Object, _translations.Object, _cache);
    }

    [Fact]
    public async Task GetAllAsync_maps_repository_rows_to_summary_dtos()
    {
        _surahs.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Surah { Id = 1, ArabicName = "ا", TransliteratedName = "Al-Fatiha", EnglishName = "Opening", EnglishNameNormalized = "opening", RevelationPlace = RevelationPlace.Meccan, AyahCount = 7 },
                new Surah { Id = 2, ArabicName = "ب", TransliteratedName = "Al-Baqarah", EnglishName = "Cow", EnglishNameNormalized = "cow", RevelationPlace = RevelationPlace.Medinan, AyahCount = 286 },
            });

        var result = await _service.GetAllAsync();
        result.Should().HaveCount(2);
        result[0].RevelationPlace.Should().Be("Meccan");
        result[1].RevelationPlace.Should().Be("Medinan");
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_when_translation_unknown()
    {
        _translations.Setup(r => r.GetByCodeAsync("unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Translation?)null);

        var result = await _service.GetByIdAsync(1, "unknown");
        result.Should().BeNull();
        _surahs.Verify(r => r.GetByIdAsync(It.IsAny<byte>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_when_surah_not_found()
    {
        _translations.Setup(r => r.GetByCodeAsync("en.sahih", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Translation { Id = 1, Code = "en.sahih", Language = "en", Name = "Saheeh", Attribution = "x" });
        _surahs.Setup(r => r.GetByIdAsync((byte)99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Surah?)null);

        var result = await _service.GetByIdAsync(99, "en.sahih");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_composes_detail_with_translation_text()
    {
        _translations.Setup(r => r.GetByCodeAsync("en.sahih", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Translation { Id = 1, Code = "en.sahih", Language = "en", Name = "Saheeh International", Attribution = "license" });
        _surahs.Setup(r => r.GetByIdAsync((byte)1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Surah { Id = 1, ArabicName = "الفاتحة", TransliteratedName = "Al-Fatiha", EnglishName = "Opening", EnglishNameNormalized = "opening", RevelationPlace = RevelationPlace.Meccan, AyahCount = 1 });
        _ayahs.Setup(r => r.GetBySurahAsync((byte)1, (byte)1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Ayah
                {
                    Id = 1, SurahId = 1, NumberInSurah = 1,
                    ArabicText = "بِسْمِ", JuzNumber = 1, HizbQuarter = 1, Sajda = false,
                    Translations = new List<AyahTranslation>
                    {
                        new() { AyahId = 1, TranslationId = 1, Text = "In the name", NormalizedText = "in the name" },
                    },
                },
            });

        var result = await _service.GetByIdAsync(1, "en.sahih");
        result.Should().NotBeNull();
        result!.Translation.Code.Should().Be("en.sahih");
        result.Ayahs.Should().HaveCount(1);
        result.Ayahs[0].TranslationText.Should().Be("In the name");
    }

    private sealed class PassThroughCache : ICachedReader
    {
        public Task<T> GetOrAddAsync<T>(string key, TimeSpan ttl, Func<CancellationToken, Task<T>> factory, CancellationToken ct = default)
            where T : class
            => factory(ct);
    }
}
