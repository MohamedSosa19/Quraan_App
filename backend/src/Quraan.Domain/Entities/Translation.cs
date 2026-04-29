namespace Quraan.Domain.Entities;

public class Translation
{
    public byte Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Attribution { get; set; } = string.Empty;

    public ICollection<AyahTranslation> AyahTranslations { get; set; } = new List<AyahTranslation>();
}
