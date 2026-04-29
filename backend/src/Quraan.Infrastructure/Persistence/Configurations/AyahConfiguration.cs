using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraan.Domain.Entities;

namespace Quraan.Infrastructure.Persistence.Configurations;

public sealed class AyahConfiguration : IEntityTypeConfiguration<Ayah>
{
    public void Configure(EntityTypeBuilder<Ayah> builder)
    {
        builder.ToTable("Ayahs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("int").ValueGeneratedNever();
        builder.Property(x => x.SurahId).HasColumnType("tinyint");
        builder.Property(x => x.NumberInSurah).HasColumnType("smallint");
        builder.Property(x => x.ArabicText).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.NormalizedArabicText).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.JuzNumber).HasColumnType("tinyint");
        builder.Property(x => x.HizbQuarter).HasColumnType("tinyint");
        builder.Property(x => x.Sajda).HasColumnType("bit");

        builder.HasOne(x => x.Surah)
            .WithMany(s => s.Ayahs)
            .HasForeignKey(x => x.SurahId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.SurahId, x.NumberInSurah })
            .IsUnique()
            .HasDatabaseName("IX_Ayah_SurahId_NumberInSurah");
        builder.HasIndex(x => x.NormalizedArabicText).HasDatabaseName("IX_Ayah_NormalizedArabicText");
    }
}
