using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraan.Domain.Entities;

namespace Quraan.Infrastructure.Persistence.Configurations;

public sealed class TafsirEntryConfiguration : IEntityTypeConfiguration<TafsirEntry>
{
    public void Configure(EntityTypeBuilder<TafsirEntry> builder)
    {
        builder.ToTable("TafsirEntries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TafsirSourceId).HasColumnType("tinyint");
        builder.Property(x => x.AyahId).HasColumnType("int");
        builder.Property(x => x.Body).HasColumnType("nvarchar(max)").IsRequired();

        builder.HasOne(x => x.Source)
            .WithMany(s => s.Entries)
            .HasForeignKey(x => x.TafsirSourceId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Ayah)
            .WithMany(a => a.TafsirEntries)
            .HasForeignKey(x => x.AyahId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TafsirSourceId, x.AyahId })
            .IsUnique()
            .HasDatabaseName("UQ_TafsirEntry_Source_Ayah");
    }
}
