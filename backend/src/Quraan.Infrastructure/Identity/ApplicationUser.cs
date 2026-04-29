using Microsoft.AspNetCore.Identity;

namespace Quraan.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? DisplayName { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSignInAt { get; set; }
}
