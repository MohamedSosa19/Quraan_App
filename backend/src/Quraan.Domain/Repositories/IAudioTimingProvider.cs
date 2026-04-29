namespace Quraan.Domain.Repositories;

public interface IAudioTimingProvider
{
    /// <summary>
    /// Fetch per-Ayah timings for a surah from quran.com (or equivalent).
    /// Returns <c>HasTimings = false</c> with an empty list when the upstream
    /// has no data or returns 404 (FR-013 — never expose stale highlights).
    /// </summary>
    /// <exception cref="UpstreamAudioFailureException">
    /// Thrown only when the upstream fails in a way that should bubble as 502
    /// to the API caller (5xx, network/timeout). Missing data is NOT a failure.
    /// </exception>
    Task<AudioTimingResult> GetTimingsAsync(int recitationId, byte surahId, CancellationToken ct = default);
}

public sealed record AudioTimingResult(bool HasTimings, IReadOnlyList<AudioAyahTiming> Timings);

public sealed record AudioAyahTiming(int NumberInSurah, int FromMs, int ToMs);

/// <summary>Signals an upstream failure that should surface as HTTP 502 (FR-014).</summary>
public sealed class UpstreamAudioFailureException : Exception
{
    public UpstreamAudioFailureException(string message) : base(message) { }
    public UpstreamAudioFailureException(string message, Exception inner) : base(message, inner) { }
}
