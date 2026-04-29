namespace Quraan.Application.Surahs;

public sealed record SurahSummaryDto(
    byte Id,
    string ArabicName,
    string TransliteratedName,
    string EnglishName,
    string RevelationPlace,
    short AyahCount);
