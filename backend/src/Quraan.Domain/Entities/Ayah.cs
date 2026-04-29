namespace Quraan.Domain.Entities;

public class Ayah
{
    public int Id { get; set; }
    public byte SurahId { get; set; }
    public Surah? Surah { get; set; }
    public short NumberInSurah { get; set; }
    public string ArabicText { get; set; } = string.Empty;
    public string NormalizedArabicText { get; set; } = string.Empty;
    public byte JuzNumber { get; set; }
    public byte HizbQuarter { get; set; }
    public bool Sajda { get; set; }

    public ICollection<AyahTranslation> Translations { get; set; } = new List<AyahTranslation>();
    public ICollection<TafsirEntry> TafsirEntries { get; set; } = new List<TafsirEntry>();
}
