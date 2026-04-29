using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraan.Domain.Entities;
using Quraan.Infrastructure.Identity;

namespace Quraan.Infrastructure.Persistence.Configurations;

public sealed class BookmarkConfiguration : IEntityTypeConfiguration<Bookmark>
{
    public void Configure(EntityTypeBuilder<Bookmark> builder)
    {
        builder.ToTable("Bookmarks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.AyahId).HasColumnType("int").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.IsDeleted).HasColumnType("bit").HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).HasColumnType("datetime2");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Ayah)
            .WithMany()
            .HasForeignKey(x => x.AyahId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.UserId, x.AyahId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UQ_Bookmark_User_Ayah");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
