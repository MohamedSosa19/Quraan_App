namespace Quraan.Application.LastRead;

public sealed record LastReadPositionDto(byte SurahId, short NumberInSurah, DateTime UpdatedAt);
