namespace Quraan.Application.Audio;

public interface IAudioService
{
    Task<AudioRecitationDto?> GetRecitationAsync(byte surahId, string reciterCode, CancellationToken ct = default);
}
