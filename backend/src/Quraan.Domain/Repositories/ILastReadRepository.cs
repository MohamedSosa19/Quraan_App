using Quraan.Domain.Entities;

namespace Quraan.Domain.Repositories;

public interface ILastReadRepository
{
    Task<LastReadPosition?> GetAsync(Guid userId, CancellationToken ct = default);
    Task UpsertAsync(LastReadPosition position, CancellationToken ct = default);
}
