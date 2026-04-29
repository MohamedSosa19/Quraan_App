namespace Quraan.Domain.Entities;

public class TafsirSource
{
    public byte Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Attribution { get; set; } = string.Empty;

    public ICollection<TafsirEntry> Entries { get; set; } = new List<TafsirEntry>();
}
