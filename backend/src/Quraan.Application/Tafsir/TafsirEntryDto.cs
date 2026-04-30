namespace Quraan.Application.Tafsir;

public sealed record TafsirEntryDto(
    byte SurahId,
    short NumberInSurah,
    TafsirSourceDto Source,
    string Body);
