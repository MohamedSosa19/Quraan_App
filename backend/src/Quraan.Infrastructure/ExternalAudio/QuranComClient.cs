using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Quraan.Domain.Repositories;

namespace Quraan.Infrastructure.ExternalAudio;

/// <summary>
/// Fetches per-Ayah timing markers from quran.com's public v4 API and maps
/// them into the domain shape consumed by AudioService.
/// FR-013: returns <c>HasTimings = false</c> on 404 / parse error so the UI
/// can render audio without highlights instead of failing the whole request.
/// FR-014: 5xx and network errors raise <see cref="UpstreamAudioFailureException"/>
/// so the controller can map to 502.
/// </summary>
public sealed class QuranComClient : IAudioTimingProvider
{
    private static readonly JsonSerializerOptions s_json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ILogger<QuranComClient> _log;

    public QuranComClient(HttpClient http, ILogger<QuranComClient> log)
    {
        _http = http;
        _log = log;
    }

    public async Task<AudioTimingResult> GetTimingsAsync(int recitationId, byte surahId, CancellationToken ct = default)
    {
        var path = $"/api/v4/recitations/{recitationId}/by_chapter/{surahId}";

        HttpResponseMessage resp;
        try
        {
            resp = await _http.GetAsync(new Uri(path, UriKind.Relative), ct).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new UpstreamAudioFailureException($"quran.com request failed: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new UpstreamAudioFailureException("quran.com request timed out.", ex);
        }

        if (resp.StatusCode == HttpStatusCode.NotFound)
            return new AudioTimingResult(false, Array.Empty<AudioAyahTiming>());

        if ((int)resp.StatusCode >= 500)
            throw new UpstreamAudioFailureException($"quran.com returned {(int)resp.StatusCode}.");

        if (!resp.IsSuccessStatusCode)
            return new AudioTimingResult(false, Array.Empty<AudioAyahTiming>());

        ChapterTimingsResponse? body;
        try
        {
            body = await ReadJsonAsync<ChapterTimingsResponse>(resp.Content, ct).ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            _log.LogWarning(ex, "quran.com response failed to parse; treating as missing timings.");
            return new AudioTimingResult(false, Array.Empty<AudioAyahTiming>());
        }

        if (body?.AudioFiles is null || body.AudioFiles.Count == 0)
            return new AudioTimingResult(false, Array.Empty<AudioAyahTiming>());

        var first = body.AudioFiles[0];
        if (first.VerseTimings is null || first.VerseTimings.Count == 0)
            return new AudioTimingResult(false, Array.Empty<AudioAyahTiming>());

        var mapped = new List<AudioAyahTiming>(first.VerseTimings.Count);
        foreach (var vt in first.VerseTimings)
        {
            // verse_key format: "78:1"
            if (string.IsNullOrEmpty(vt.VerseKey)) continue;
            var idx = vt.VerseKey.IndexOf(':', StringComparison.Ordinal);
            if (idx < 1 || idx == vt.VerseKey.Length - 1) continue;
            if (!int.TryParse(vt.VerseKey.AsSpan(idx + 1), out var num)) continue;

            mapped.Add(new AudioAyahTiming(num, vt.Timestamp_From, vt.Timestamp_To));
        }

        return new AudioTimingResult(mapped.Count > 0, mapped);
    }

    private sealed record ChapterTimingsResponse(
        [property: JsonPropertyName("audio_files")] IList<AudioFile>? AudioFiles);

    private sealed record AudioFile(
        [property: JsonPropertyName("verse_timings")] IList<VerseTiming>? VerseTimings);

    private sealed record VerseTiming(
        [property: JsonPropertyName("verse_key")] string? VerseKey,
        [property: JsonPropertyName("timestamp_from")] int Timestamp_From,
        [property: JsonPropertyName("timestamp_to")] int Timestamp_To);

    private static async Task<T?> ReadJsonAsync<T>(HttpContent content, CancellationToken ct)
    {
        await using var stream = await content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        return await JsonSerializer.DeserializeAsync<T>(stream, s_json, ct).ConfigureAwait(false);
    }
}
