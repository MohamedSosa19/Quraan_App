namespace Quraan.Domain.Entities;

public class AyahTranslation
{
    public int AyahId { get; set; }
    public Ayah? Ayah { get; set; }
    public byte TranslationId { get; set; }
    public Translation? Translation { get; set; }
    public string Text { get; set; } = string.Empty;
    public string NormalizedText { get; set; } = string.Empty;
}
