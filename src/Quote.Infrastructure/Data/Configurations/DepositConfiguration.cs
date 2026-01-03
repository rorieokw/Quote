using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quote.Domain.Entities;

namespace Quote.Infrastructure.Data.Configurations;

public class DepositConfiguration : IEntityTypeConfiguration<Deposit>
{
    public void Configure(EntityTypeBuilder<Deposit> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Amount)
            .HasPrecision(18, 2);

        builder.Property(d => d.PercentageOfTotal)
            .HasPrecision(5, 2);

        builder.Property(d => d.StripePaymentIntentId)
            .HasMaxLength(100);

        builder.Property(d => d.RefundReason)
            .HasMaxLength(500);

        builder.HasOne(d => d.Quote)
            .WithMany()
            .HasForeignKey(d => d.JobQuoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => d.JobQuoteId);
        builder.HasIndex(d => d.Status);
    }
}
