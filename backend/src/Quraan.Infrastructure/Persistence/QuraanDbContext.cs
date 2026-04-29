using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Quraan.Domain.Entities;
using Quraan.Infrastructure.Identity;

namespace Quraan.Infrastructure.Persistence;

public class QuraanDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public QuraanDbContext(DbContextOptions<QuraanDbContext> options) : base(options) { }

    public DbSet<Surah> Surahs => Set<Surah>();
    public DbSet<Ayah> Ayahs => Set<Ayah>();
    public DbSet<Translation> Translations => Set<Translation>();
    public DbSet<AyahTranslation> AyahTranslations => Set<AyahTranslation>();
    public DbSet<TafsirSource> TafsirSources => Set<TafsirSource>();
    public DbSet<TafsirEntry> TafsirEntries => Set<TafsirEntry>();
    public DbSet<Reciter> Reciters => Set<Reciter>();
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<LastReadPosition> LastReadPositions => Set<LastReadPosition>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(QuraanDbContext).Assembly);
    }
}
