using Quraan.Application.Audio;
using Quraan.Domain.Entities;

namespace Quraan.Infrastructure.ExternalAudio;

/// <summary>
/// Composes Al Quran Cloud CDN URLs for per-surah audio streams (R-04).
/// No HTTP call is made server-side — the browser streams directly from the CDN
/// using the URL we hand back. Kept as an injectable service so tests can
/// substitute a deterministic stub and the URL pattern lives in one place.
/// </summary>
public sealed class AlQuranCloudClient : IAudioUrlBuilder
{
    /// <summary>Default CDN bitrate. R-04 pins 128 kbps for Alafasy.</summary>
    public const int DefaultBitrate = 128;

    public string BuildSurahAudioUrl(Reciter reciter, byte surahId)
    {
        ArgumentNullException.ThrowIfNull(reciter);
        if (string.IsNullOrWhiteSpace(reciter.AlQuranCloudId))
            throw new InvalidOperationException($"Reciter {reciter.Code} has no AlQuranCloudId mapping.");
        if (surahId is < 1 or > 114)
            throw new ArgumentOutOfRangeException(nameof(surahId), "surahId must be between 1 and 114.");

        return $"https://cdn.islamic.network/quran/audio-surah/{DefaultBitrate}/{reciter.AlQuranCloudId}/{surahId}.mp3";
    }
}
