using Quraan.Application.Caching;
using Quraan.Application.Surahs;
using Quraan.Domain.Common;
using Quraan.Domain.Entities;
using Quraan.Domain.Repositories;

namespace Quraan.Application.Search;

public sealed class SearchService : ISearchService
{
    public const int MaxPageSize = 100;
    public const int SnippetWidth = 60;

    private readonly ISurahRepository _surahs;
    private readonly IAyahRepository _ayahs;
    private readonly ITranslationRepository _translations;
    private readonly ICachedReader _cache;

    public SearchService(
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

    public async Task<SearchResponseDto> SearchAsync(string query, int page, int pageSize, string translationCode, CancellationToken ct = default)
    {
        var trimmed = (query ?? string.Empty).Trim();
        if (trimmed.Length < 1)
            throw new ArgumentException("Query must contain at least one character.", nameof(query));

        page = page < 1 ? 1 : page;
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var translation = await _translations.GetByCodeAsync(translationCode, ct).ConfigureAwait(false);
        var translationId = translation?.Id ?? (byte)1;
        var translationCodeKey = translation?.Code ?? translationCode;
        var normalized = ArabicNormalizer.ForQuery(trimmed);

        var key = CacheKeyFactory.Search(normalized, page, pageSize, translationCodeKey);
        return await _cache.GetOrAddAsync(
            key,
            CachedReader.SearchTtl,
            async ctx => await BuildAsync(trimmed, normalized, translationId, page, pageSize, ctx).ConfigureAwait(false),
            ct).ConfigureAwait(false);
    }

    private async Task<SearchResponseDto> BuildAsync(string original, string normalized, byte translationId, int page, int pageSize, CancellationToken ct)
    {
        var surahsTask = _surahs.SearchByNameAsync(normalized, ct);
        var arabicTask = _ayahs.SearchArabicAsync(normalized, 0, MaxPageSize, ct);
        var translationTask = _ayahs.SearchTranslationAsync(normalized, translationId, 0, MaxPageSize, ct);
        var arabicCountTask = _ayahs.CountArabicSearchAsync(normalized, ct);
        var translationCountTask = _ayahs.CountSearchAsync(normalized, translationId, ct);

        await Task.WhenAll(surahsTask, arabicTask, translationTask, arabicCountTask, translationCountTask).ConfigureAwait(false);

        var surahMatches = (await surahsTask).Select(MapSummary).ToList();

        var arabicMatches = (await arabicTask)
            .Select(a => MapMatch(a, "arabic", original, normalized, translationId))
            .ToList();
        var translationMatches = (await translationTask)
            .Select(a => MapMatch(a, "translation", original, normalized, translationId))
            .ToList();

        // Union by Ayah.Id, preferring the Arabic match label when both fired.
        var seen = new HashSet<int>(arabicMatches.Count + translationMatches.Count);
        var unionOrdered = new List<AyahMatchDto>(arabicMatches.Count + translationMatches.Count);
        foreach (var m in arabicMatches.Concat(translationMatches))
        {
            if (seen.Add(m.Id)) unionOrdered.Add(m);
        }
        unionOrdered.Sort(static (x, y) =>
        {
            var c = x.SurahId.CompareTo(y.SurahId);
            return c != 0 ? c : x.NumberInSurah.CompareTo(y.NumberInSurah);
        });

        var totalAyahMatches = unionOrdered.Count;
        var skip = (page - 1) * pageSize;
        var pageItems = unionOrdered.Skip(skip).Take(pageSize).ToList();

        return new SearchResponseDto(
            original,
            surahMatches,
            pageItems,
            page,
            pageSize,
            totalAyahMatches);
    }

    private static SurahSummaryDto MapSummary(Surah s) =>
        new(s.Id, s.ArabicName, s.TransliteratedName, s.EnglishName, s.RevelationPlace.ToString(), s.AyahCount);

    private static AyahMatchDto MapMatch(Ayah a, string matchedIn, string original, string normalized, byte translationId)
    {
        var translation = a.Translations.FirstOrDefault(t => t.TranslationId == translationId)?.Text ?? string.Empty;
        var snippetSource = matchedIn == "arabic" ? a.ArabicText : translation;
        var snippet = BuildSnippet(snippetSource, original, normalized);
        return new AyahMatchDto(
            a.Id, a.SurahId, a.NumberInSurah,
            a.ArabicText, translation,
            a.JuzNumber, a.HizbQuarter, a.Sajda,
            matchedIn, snippet);
    }

    /// <summary>
    /// Build a 60-character snippet roughly centered on the first match. Matching
    /// is best-effort: tries the original query (case-insensitive), then the
    /// normalized form, then falls back to the leading prefix.
    /// </summary>
    public static string BuildSnippet(string source, string original, string normalized)
    {
        if (string.IsNullOrEmpty(source)) return string.Empty;
        if (source.Length <= SnippetWidth) return source;

        var idx = -1;
        if (!string.IsNullOrEmpty(original))
            idx = source.IndexOf(original, StringComparison.OrdinalIgnoreCase);
        if (idx < 0 && !string.IsNullOrEmpty(normalized))
            idx = source.IndexOf(normalized, StringComparison.OrdinalIgnoreCase);

        if (idx < 0) return source[..SnippetWidth] + "…";

        var half = SnippetWidth / 2;
        var start = Math.Max(0, idx - half);
        var end = Math.Min(source.Length, start + SnippetWidth);
        var prefix = start == 0 ? string.Empty : "…";
        var suffix = end == source.Length ? string.Empty : "…";
        return prefix + source[start..end] + suffix;
    }
}
