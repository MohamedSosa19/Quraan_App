using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraan.Domain.Entities;

namespace Quraan.Infrastructure.Persistence.Configurations;

public sealed class TranslationConfiguration : IEntityTypeConfiguration<Translation>
{
    public void Configure(EntityTypeBuilder<Translation> builder)
    {
        builder.ToTable("Translations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("tinyint").ValueGeneratedNever();
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Language).HasMaxLength(8).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Attribution).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}
