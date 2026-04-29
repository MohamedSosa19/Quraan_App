using Microsoft.EntityFrameworkCore;
using Quraan.Domain.Entities;
using Quraan.Domain.Repositories;
using Quraan.Infrastructure.Persistence;

namespace Quraan.Infrastructure.Repositories;

public sealed class SurahRepository : ISurahRepository
{
    private readonly QuraanDbContext _db;
    public SurahRepository(QuraanDbContext db) => _db = db;

    public async Task<IReadOnlyList<Surah>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Surahs.AsNoTracking().OrderBy(s => s.Id).ToListAsync(ct).ConfigureAwait(false);

    public Task<Surah?> GetByIdAsync(byte id, CancellationToken ct = default) =>
        _db.Surahs.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<Surah>> SearchByNameAsync(string normalizedQuery, CancellationToken ct = default)
    {
        var pattern = $"%{normalizedQuery}%";
        return await _db.Surahs.AsNoTracking()
            .Where(s => EF.Functions.Like(s.EnglishNameNormalized, pattern)
                     || EF.Functions.Like(s.TransliteratedName, pattern)
                     || EF.Functions.Like(s.ArabicName, pattern))
            .OrderBy(s => s.Id)
            .ToListAsync(ct).ConfigureAwait(false);
    }
}
