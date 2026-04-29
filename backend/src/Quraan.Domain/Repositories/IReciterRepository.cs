using Quraan.Domain.Entities;

namespace Quraan.Domain.Repositories;

public interface IReciterRepository
{
    Task<Reciter?> GetByCodeAsync(string code, CancellationToken ct = default);
}
