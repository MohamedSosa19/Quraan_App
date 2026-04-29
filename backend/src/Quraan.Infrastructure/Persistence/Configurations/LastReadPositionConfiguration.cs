using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraan.Domain.Entities;
using Quraan.Infrastructure.Identity;

namespace Quraan.Infrastructure.Persistence.Configurations;

public sealed class LastReadPositionConfiguration : IEntityTypeConfiguration<LastReadPosition>
{
    public void Configure(EntityTypeBuilder<LastReadPosition> builder)
    {
        builder.ToTable("LastReadPositions");
        builder.HasKey(x => x.UserId);
        builder.Property(x => x.UserId).ValueGeneratedNever();
        builder.Property(x => x.SurahId).HasColumnType("tinyint").IsRequired();
        builder.Property(x => x.AyahNumberInSurah).HasColumnType("smallint").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<LastReadPosition>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Surah)
            .WithMany()
            .HasForeignKey(x => x.SurahId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
