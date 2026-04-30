namespace Quraan.Application.LastRead;

public interface ILastReadService
{
    Task<LastReadPositionDto?> GetAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Last-write-wins by <c>UpdatedAt</c> (FR-035, R-13). If the incoming
    /// position's <c>UpdatedAt</c> is strictly newer than the stored value,
    /// the row is replaced; otherwise the stored value is preserved. Returns
    /// the resulting (winning) position.
    /// </summary>
    Task<LastReadPositionDto> UpsertAsync(Guid userId, LastReadPositionDto incoming, CancellationToken ct = default);
}
