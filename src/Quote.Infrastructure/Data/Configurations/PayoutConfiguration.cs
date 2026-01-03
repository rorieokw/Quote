using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quote.Domain.Entities;

namespace Quote.Infrastructure.Data.Configurations;

public class PayoutConfiguration : IEntityTypeConfiguration<Payout>
{
    public void Configure(EntityTypeBuilder<Payout> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.StripePayoutId)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(p => p.StripePayoutId)
            .IsUnique();

        builder.Property(p => p.Amount)
            .HasPrecision(18, 2);

        builder.Property(p => p.Currency)
            .HasMaxLength(10)
            .HasDefaultValue("aud");

        builder.Property(p => p.FailureCode)
            .HasMaxLength(100);

        builder.Property(p => p.FailureMessage)
            .HasMaxLength(500);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.StatementDescriptor)
            .HasMaxLength(22);

        builder.HasOne(p => p.Tradie)
            .WithMany()
            .HasForeignKey(p => p.TradieId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
