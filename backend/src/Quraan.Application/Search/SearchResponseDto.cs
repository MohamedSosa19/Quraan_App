using System.Collections.Generic;
using Quraan.Application.Surahs;

namespace Quraan.Application.Search;

public sealed record SearchResponseDto(
    string Query,
    IReadOnlyList<SurahSummaryDto> SurahMatches,
    IReadOnlyList<AyahMatchDto> AyahMatches,
    int Page,
    int PageSize,
    int TotalAyahMatches);
