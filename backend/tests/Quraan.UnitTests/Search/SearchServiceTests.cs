using FluentAssertions;
using Moq;
using Quraan.Application.Caching;
using Quraan.Application.Search;
using Quraan.Domain.Common;
using Quraan.Domain.Entities;
using Quraan.Domain.Repositories;
using Xunit;

namespace Quraan.UnitTests.Search;

public sealed class SearchServiceTests
{
    private readonly Mock<ISurahRepository> _surahs = new();
    private readonly Mock<IAyahRepository> _ayahs = new();
    private readonly Mock<ITranslationRepository> _translations = new();
    private readonly PassThroughCache _cache = new();
    private readonly SearchService _service;

    public SearchServiceTests()
    {
        _translations.Setup(r => r.GetByCodeAsync("en.sahih", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Translation { Id = 1, Code = "en.sahih", Language = "en", Name = "Saheeh", Attribution = "x" });
        _surahs.Setup(r => r.SearchByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Surah>());
        _ayahs.Setup(r => r.SearchArabicAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Ayah>());
        _ayahs.Setup(r => r.SearchTranslationAsync(It.IsAny<string>(), It.IsAny<byte>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Ayah>());
        _ayahs.Setup(r => r.CountArabicSearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _ayahs.Setup(r => r.CountSearchAsync(It.IsAny<string>(), It.IsAny<byte>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

        _service = new SearchService(_surahs.Object, _ayahs.Object, _translations.Object, _cache);
    }

    [Fact]
    public async Task Empty_query_throws_argument_exception()
    {
        var act = async () => await _service.SearchAsync("   ", 1, 20, "en.sahih");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Single_character_query_is_accepted()
    {
        var act = async () => await _service.SearchAsync("a", 1, 20, "en.sahih");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Trims_whitespace_in_query_before_normalizing()
    {
        await _service.SearchAsync("  Mercy  ", 1, 20, "en.sahih");
        // Normalized form is "mercy" — verify the repo received the trimmed/lowercased pattern.
        _ayahs.Verify(r => r.SearchTranslationAsync(
            "mercy", (byte)1, 0, SearchService.MaxPageSize, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PageSize_is_capped_at_100()
    {
        var result = await _service.SearchAsync("x", 1, 500, "en.sahih");
        result.PageSize.Should().Be(SearchService.MaxPageSize);
    }

    [Fact]
    public async Task Page_below_1_is_clamped_to_1()
    {
        var result = await _service.SearchAsync("x", -3, 20, "en.sahih");
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task English_query_lowercased_before_translation_search()
    {
        await _service.SearchAsync("MERCY", 1, 20, "en.sahih");
        _ayahs.Verify(r => r.SearchTranslationAsync("mercy", (byte)1, 0, SearchService.MaxPageSize, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Arabic_match_takes_priority_over_translation_for_same_ayah()
    {
        var ayah = new Ayah
        {
            Id = 100, SurahId = 2, NumberInSurah = 1,
            ArabicText = "ا", NormalizedArabicText = "ا",
            Translations = new List<AyahTranslation>
            {
                new() { AyahId = 100, TranslationId = 1, Text = "x", NormalizedText = "x" },
            },
        };
        _ayahs.Setup(r => r.SearchArabicAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { ayah });
        _ayahs.Setup(r => r.SearchTranslationAsync(It.IsAny<string>(), It.IsAny<byte>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { ayah });

        var result = await _service.SearchAsync("a", 1, 20, "en.sahih");
        result.AyahMatches.Should().HaveCount(1);
        result.AyahMatches[0].MatchedIn.Should().Be("arabic");
    }

    [Fact]
    public async Task Surah_matches_returned_when_repo_finds_them()
    {
        _surahs.Setup(r => r.SearchByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Surah { Id = 36, ArabicName = "يس", TransliteratedName = "Ya-Sin", EnglishName = "Ya Sin", EnglishNameNormalized = "ya sin", RevelationPlace = RevelationPlace.Meccan, AyahCount = 83 },
            });

        var result = await _service.SearchAsync("ya sin", 1, 20, "en.sahih");
        result.SurahMatches.Should().HaveCount(1);
        result.SurahMatches[0].Id.Should().Be(36);
    }

    [Fact]
    public void BuildSnippet_centers_on_match_under_60_chars()
    {
        var src = new string('x', 200) + "MERCY" + new string('y', 200);
        var snip = SearchService.BuildSnippet(src, "MERCY", "mercy");
        snip.Should().Contain("MERCY");
        snip.Length.Should().BeLessThanOrEqualTo(SearchService.SnippetWidth + 2); // +2 for the ellipses
    }

    [Fact]
    public void BuildSnippet_returns_full_text_when_shorter_than_window()
    {
        SearchService.BuildSnippet("short text", "text", "text").Should().Be("short text");
    }

    private sealed class PassThroughCache : ICachedReader
    {
        public Task<T> GetOrAddAsync<T>(string key, TimeSpan ttl, Func<CancellationToken, Task<T>> factory, CancellationToken ct = default) where T : class
            => factory(ct);
    }
}
