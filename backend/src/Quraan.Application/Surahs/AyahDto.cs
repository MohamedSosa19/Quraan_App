namespace Quraan.Application.Surahs;

public record AyahDto(
    int Id,
    byte SurahId,
    short NumberInSurah,
    string ArabicText,
    string TranslationText,
    byte JuzNumber,
    byte HizbQuarter,
    bool Sajda);
