using Quraan.Domain.Entities;

namespace Quraan.Domain.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FindByHashAsync(byte[] tokenHash, CancellationToken ct = default);
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task UpdateAsync(RefreshToken token, CancellationToken ct = default);
    Task RevokeChainAsync(Guid userId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
