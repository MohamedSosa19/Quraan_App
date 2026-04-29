using Quraan.Domain.Entities;

namespace Quraan.Domain.Repositories;

public interface ITafsirRepository
{
    Task<TafsirEntry?> GetForAyahAsync(byte surahId, short numberInSurah, byte tafsirSourceId, CancellationToken ct = default);
}
