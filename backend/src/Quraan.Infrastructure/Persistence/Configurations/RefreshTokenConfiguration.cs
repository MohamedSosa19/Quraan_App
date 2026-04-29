using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraan.Domain.Entities;
using Quraan.Infrastructure.Identity;

namespace Quraan.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.TokenHash).HasColumnType("binary(32)").IsRequired();
        builder.Property(x => x.IssuedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.RevokedAt).HasColumnType("datetime2");
        builder.Property(x => x.ReplacedByTokenHash).HasColumnType("binary(32)");
        builder.Property(x => x.CreatedByIp).HasMaxLength(64);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId).HasDatabaseName("IX_RefreshToken_UserId");
        builder.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("UQ_RefreshToken_TokenHash");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
