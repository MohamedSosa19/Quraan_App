namespace Quraan.Application.Tafsir;

public interface ITafsirService
{
    Task<TafsirEntryDto?> GetForAyahAsync(byte surahId, short numberInSurah, string sourceCode, CancellationToken ct = default);
}
