namespace Quraan.Application.Caching;

public static class CacheKeyFactory
{
    public const string Prefix = "quraan:v1:";

    public static string Surahs() => $"{Prefix}surahs:all";
    public static string Surah(byte id, string translation) => $"{Prefix}surah:{id}:{translation}";
    public static string Ayah(byte surahId, short n, string translation) => $"{Prefix}ayah:{surahId}:{n}:{translation}";
    public static string Tafsir(byte surahId, short n, string source) => $"{Prefix}tafsir:{surahId}:{n}:{source}";
    public static string Audio(string reciter, byte surahId) => $"{Prefix}audio:{reciter}:{surahId}";
    public static string Search(string normalizedQuery, int page, int pageSize, string translation) =>
        $"{Prefix}search:{translation}:{normalizedQuery}:{page}:{pageSize}";
}
