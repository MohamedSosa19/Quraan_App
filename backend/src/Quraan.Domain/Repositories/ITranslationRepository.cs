using Quraan.Domain.Entities;

namespace Quraan.Domain.Repositories;

public interface ITranslationRepository
{
    Task<Translation?> GetByCodeAsync(string code, CancellationToken ct = default);
}
