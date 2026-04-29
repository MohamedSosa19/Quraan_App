using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraan.Domain.Entities;

namespace Quraan.Infrastructure.Persistence.Configurations;

public sealed class SurahConfiguration : IEntityTypeConfiguration<Surah>
{
    public void Configure(EntityTypeBuilder<Surah> builder)
    {
        builder.ToTable("Surahs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("tinyint").ValueGeneratedNever();
        builder.Property(x => x.ArabicName).HasMaxLength(64).IsRequired();
        builder.Property(x => x.TransliteratedName).HasMaxLength(64).IsRequired();
        builder.Property(x => x.EnglishName).HasMaxLength(64).IsRequired();
        builder.Property(x => x.EnglishNameNormalized).HasMaxLength(64).IsRequired();
        builder.Property(x => x.RevelationPlace).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(x => x.AyahCount).HasColumnType("smallint");
        builder.Property(x => x.OrderInRevelation).HasColumnType("tinyint");

        builder.HasIndex(x => x.EnglishNameNormalized).HasDatabaseName("IX_Surah_EnglishNameNormalized");
        builder.HasIndex(x => x.TransliteratedName).HasDatabaseName("IX_Surah_TransliteratedName");
    }
}
