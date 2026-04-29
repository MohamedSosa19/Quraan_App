namespace Quraan.Domain.Entities;

public class Reciter
{
    public byte Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ArabicName { get; set; } = string.Empty;
    public string AlQuranCloudId { get; set; } = string.Empty;
    public int QuranComId { get; set; }
    public string Attribution { get; set; } = string.Empty;
}
