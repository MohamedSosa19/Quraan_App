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
        (from e in _db.TafsirEntries.AsNoTracking().Include(e => e.Source)
         join a in _db.Ayahs.AsNoTracking() on e.AyahId equals a.Id
         where e.TafsirSourceId == tafsirSourceId
            && a.SurahId == surahId
            && a.NumberInSurah == numberInSurah
         select e).FirstOrDefaultAsync(ct);

    public Task<TafsirSource?> GetSourceByCodeAsync(string code, CancellationToken ct = default) =>
        _db.TafsirSources.AsNoTracking().FirstOrDefaultAsync(s => s.Code == code, ct);
}
