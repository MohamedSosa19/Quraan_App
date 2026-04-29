using Microsoft.EntityFrameworkCore;
using Quraan.Domain.Entities;
using Quraan.Domain.Repositories;
using Quraan.Infrastructure.Persistence;

namespace Quraan.Infrastructure.Repositories;

public sealed class TranslationRepository : ITranslationRepository
{
    private readonly QuraanDbContext _db;
    public TranslationRepository(QuraanDbContext db) => _db = db;

    public Task<Translation?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        _db.Translations.AsNoTracking().FirstOrDefaultAsync(t => t.Code == code, ct);
}
