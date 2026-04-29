using System.Collections.Generic;

namespace Quraan.Application.Surahs;

public sealed record SurahDetailDto(
    byte Id,
    string ArabicName,
    string TransliteratedName,
    string EnglishName,
    string RevelationPlace,
    short AyahCount,
    TranslationInfoDto Translation,
    IReadOnlyList<AyahDto> Ayahs);
