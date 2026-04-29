namespace Quraan.Application.Surahs;

public sealed record TranslationInfoDto(
    string Code,
    string Language,
    string Name,
    string Attribution);
