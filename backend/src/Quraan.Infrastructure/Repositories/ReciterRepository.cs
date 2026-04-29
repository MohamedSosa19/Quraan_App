using Microsoft.EntityFrameworkCore;
using Quraan.Domain.Entities;
using Quraan.Domain.Repositories;
using Quraan.Infrastructure.Persistence;

namespace Quraan.Infrastructure.Repositories;

public sealed class ReciterRepository : IReciterRepository
{
    private readonly QuraanDbContext _db;
    public ReciterRepository(QuraanDbContext db) => _db = db;

    public Task<Reciter?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        _db.Reciters.AsNoTracking().FirstOrDefaultAsync(r => r.Code == code, ct);
}
