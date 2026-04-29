namespace Quraan.Domain.Entities;

public class TafsirEntry
{
    public int Id { get; set; }
    public byte TafsirSourceId { get; set; }
    public TafsirSource? Source { get; set; }
    public int AyahId { get; set; }
    public Ayah? Ayah { get; set; }
    public string Body { get; set; } = string.Empty;
}
