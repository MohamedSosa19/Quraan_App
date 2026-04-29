using Quraan.Domain.Common;

namespace Quraan.Domain.Entities;

public class Surah
{
    public byte Id { get; set; }
    public string ArabicName { get; set; } = string.Empty;
    public string TransliteratedName { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;
    public string EnglishNameNormalized { get; set; } = string.Empty;
    public RevelationPlace RevelationPlace { get; set; }
    public short AyahCount { get; set; }
    public byte OrderInRevelation { get; set; }

    public ICollection<Ayah> Ayahs { get; set; } = new List<Ayah>();
}
