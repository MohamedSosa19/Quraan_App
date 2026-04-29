using Quraan.Domain.Entities;

namespace Quraan.Domain.Repositories;

public interface ISurahRepository
{
    Task<IReadOnlyList<Surah>> GetAllAsync(CancellationToken ct = default);
    Task<Surah?> GetByIdAsync(byte id, CancellationToken ct = default);
    Task<IReadOnlyList<Surah>> SearchByNameAsync(string normalizedQuery, CancellationToken ct = default);
}
