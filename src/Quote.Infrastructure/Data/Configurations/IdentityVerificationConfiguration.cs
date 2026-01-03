using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quote.Domain.Entities;

namespace Quote.Infrastructure.Data.Configurations;

public class IdentityVerificationConfiguration : IEntityTypeConfiguration<IdentityVerification>
{
    public void Configure(EntityTypeBuilder<IdentityVerification> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.DocumentFrontUrl).HasMaxLength(500);
        builder.Property(i => i.DocumentBackUrl).HasMaxLength(500);
        builder.Property(i => i.DocumentNumber).HasMaxLength(50);
        builder.Property(i => i.IssuingState).HasMaxLength(50);
        builder.Property(i => i.VerificationNotes).HasMaxLength(1000);

        builder.HasOne(i => i.User)
            .WithMany()
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.ReviewedBy)
            .WithMany()
            .HasForeignKey(i => i.ReviewedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(i => i.UserId);
        builder.HasIndex(i => i.VerificationStatus);
        builder.HasIndex(i => new { i.UserId, i.DocumentType });
    }
}
