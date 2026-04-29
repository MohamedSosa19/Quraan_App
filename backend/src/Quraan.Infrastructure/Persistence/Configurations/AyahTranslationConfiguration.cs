using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraan.Domain.Entities;

namespace Quraan.Infrastructure.Persistence.Configurations;

public sealed class AyahTranslationConfiguration : IEntityTypeConfiguration<AyahTranslation>
{
    public void Configure(EntityTypeBuilder<AyahTranslation> builder)
    {
        builder.ToTable("AyahTranslations");
        builder.HasKey(x => new { x.AyahId, x.TranslationId });
        builder.Property(x => x.AyahId).HasColumnType("int");
        builder.Property(x => x.TranslationId).HasColumnType("tinyint");
        builder.Property(x => x.Text).HasMaxLength(2048).IsRequired();
        builder.Property(x => x.NormalizedText).HasMaxLength(2048).IsRequired();

        builder.HasOne(x => x.Ayah)
            .WithMany(a => a.Translations)
            .HasForeignKey(x => x.AyahId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Translation)
            .WithMany(t => t.AyahTranslations)
            .HasForeignKey(x => x.TranslationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.NormalizedText).HasDatabaseName("IX_AyahTranslation_NormalizedText");
    }
}
