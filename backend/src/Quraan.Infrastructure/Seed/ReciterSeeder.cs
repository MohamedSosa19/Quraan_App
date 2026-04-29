using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quraan.Domain.Entities;
using Quraan.Infrastructure.Persistence;

namespace Quraan.Infrastructure.Seed;

public sealed class ReciterSeeder
{
    private readonly QuraanDbContext _db;
    private readonly ILogger<ReciterSeeder> _log;

    public ReciterSeeder(QuraanDbContext db, ILogger<ReciterSeeder> log)
    {
        _db = db;
        _log = log;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await _db.Reciters.AnyAsync(ct).ConfigureAwait(false))
        {
            _log.LogInformation("Reciters already seeded; skipping.");
            return;
        }

        _db.Reciters.Add(new Reciter
        {
            Id = 1,
            Code = "ar.alafasy",
            Name = "Mishary Rashid Alafasy",
            ArabicName = "مشاري راشد العفاسي",
            AlQuranCloudId = "ar.alafasy",
            QuranComId = 7,
            Attribution = "Recitations courtesy Al Quran Cloud + quran.com (Mishary Rashid Alafasy)."
        });

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _log.LogInformation("Seeded reciter Alafasy (id=1).");
    }
}
