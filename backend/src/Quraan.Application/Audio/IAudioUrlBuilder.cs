using Quraan.Domain.Entities;

namespace Quraan.Application.Audio;

/// <summary>
/// Application-layer view of <c>AlQuranCloudClient</c>. Lets <see cref="AudioService"/>
/// stay free of an Infrastructure reference (Constitution II).
/// </summary>
public interface IAudioUrlBuilder
{
    string BuildSurahAudioUrl(Reciter reciter, byte surahId);
}
