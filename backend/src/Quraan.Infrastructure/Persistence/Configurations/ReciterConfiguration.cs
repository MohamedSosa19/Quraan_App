using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraan.Domain.Entities;

namespace Quraan.Infrastructure.Persistence.Configurations;

public sealed class ReciterConfiguration : IEntityTypeConfiguration<Reciter>
{
    public void Configure(EntityTypeBuilder<Reciter> builder)
    {
        builder.ToTable("Reciters");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("tinyint").ValueGeneratedNever();
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ArabicName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.AlQuranCloudId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.QuranComId).HasColumnType("int");
        builder.Property(x => x.Attribution).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}
