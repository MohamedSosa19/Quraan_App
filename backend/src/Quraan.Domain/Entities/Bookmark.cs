namespace Quraan.Domain.Entities;

public class Bookmark
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int AyahId { get; set; }
    public Ayah? Ayah { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
