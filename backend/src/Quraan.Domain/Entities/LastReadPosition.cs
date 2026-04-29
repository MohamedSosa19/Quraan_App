namespace Quraan.Domain.Entities;

public class LastReadPosition
{
    public Guid UserId { get; set; }
    public byte SurahId { get; set; }
    public Surah? Surah { get; set; }
    public short AyahNumberInSurah { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
