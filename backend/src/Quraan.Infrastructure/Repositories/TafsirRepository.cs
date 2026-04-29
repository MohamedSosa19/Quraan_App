using Microsoft.EntityFrameworkCore;
using Quraan.Domain.Entities;
using Quraan.Domain.Repositories;
using Quraan.Infrastructure.Persistence;

namespace Quraan.Infrastructure.Repositories;

public sealed class TafsirRepository : ITafsirRepository
{
    private readonly QuraanDbContext _db;
    public TafsirRepository(QuraanDbContext db) => _db = db;

    public Task<TafsirEntry?> GetForAyahAsync(byte surahId, short numberInSurah, byte tafsirSourceId, CancellationToken ct = default) =>
        _db.TafsirEntries.AsNoTracking()
            .Include(e => e.Source)
            .Where(e => e.TafsirSourceId == tafsirSourceId
                     && e.Ayah!.SurahId == surahId
                     && e.Ayah.NumberInSurah == numberInSurah)
            .FirstOrDefaultAsync(ct);
}
