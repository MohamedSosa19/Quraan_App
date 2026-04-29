namespace Quraan.Application.Search;

public sealed record AyahMatchDto(
    int Id,
    byte SurahId,
    short NumberInSurah,
    string ArabicText,
    string TranslationText,
    byte JuzNumber,
    byte HizbQuarter,
    bool Sajda,
    string MatchedIn,
    string HighlightSnippet);
