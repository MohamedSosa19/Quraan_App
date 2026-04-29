using Quraan.Application.Surahs;

namespace Quraan.Application.Ayahs;

public interface IAyahService
{
    Task<AyahDto?> GetAsync(byte surahId, short numberInSurah, string translationCode, CancellationToken ct = default);
}
