using Quraan.Domain.Entities;
using Quraan.Domain.Repositories;

namespace Quraan.Application.LastRead;

public sealed class LastReadService : ILastReadService
{
    private readonly ILastReadRepository _repo;
    public LastReadService(ILastReadRepository repo) => _repo = repo;

    public async Task<LastReadPositionDto?> GetAsync(Guid userId, CancellationToken ct = default)
    {
        var pos = await _repo.GetAsync(userId, ct).ConfigureAwait(false);
        return pos is null ? null : new LastReadPositionDto(pos.SurahId, pos.AyahNumberInSurah, pos.UpdatedAt);
    }

    public async Task<LastReadPositionDto> UpsertAsync(Guid userId, LastReadPositionDto incoming, CancellationToken ct = default)
    {
        if (incoming.SurahId is < 1 or > 114)
            throw new ArgumentOutOfRangeException(nameof(incoming), "surahId must be between 1 and 114.");
        if (incoming.NumberInSurah < 1)
            throw new ArgumentOutOfRangeException(nameof(incoming), "numberInSurah must be ≥ 1.");

        await _repo.UpsertAsync(new LastReadPosition
        {
            UserId = userId,
            SurahId = incoming.SurahId,
            AyahNumberInSurah = incoming.NumberInSurah,
            UpdatedAt = incoming.UpdatedAt == default ? DateTime.UtcNow : incoming.UpdatedAt,
        }, ct).ConfigureAwait(false);

        // Return whichever value won the merge.
        var winner = await _repo.GetAsync(userId, ct).ConfigureAwait(false);
        return winner is null
            ? incoming
            : new LastReadPositionDto(winner.SurahId, winner.AyahNumberInSurah, winner.UpdatedAt);
    }
}
