using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quote.Domain.Entities;

namespace Quote.Infrastructure.Data.Configurations;

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.StripePaymentIntentId)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(t => t.StripePaymentIntentId)
            .IsUnique();

        builder.Property(t => t.StripeChargeId)
            .HasMaxLength(255);

        builder.Property(t => t.Amount)
            .HasPrecision(18, 2);

        builder.Property(t => t.PlatformFee)
            .HasPrecision(18, 2);

        builder.Property(t => t.TradiePayout)
            .HasPrecision(18, 2);

        builder.Property(t => t.RefundedAmount)
            .HasPrecision(18, 2);

        builder.Property(t => t.Currency)
            .HasMaxLength(10)
            .HasDefaultValue("aud");

        builder.Property(t => t.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.PaymentMethodType)
            .HasMaxLength(50);

        builder.Property(t => t.ReceiptEmail)
            .HasMaxLength(255);

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.Property(t => t.FailureCode)
            .HasMaxLength(100);

        builder.Property(t => t.FailureMessage)
            .HasMaxLength(500);

        builder.HasOne(t => t.Customer)
            .WithMany()
            .HasForeignKey(t => t.CustomerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(t => t.Tradie)
            .WithMany()
            .HasForeignKey(t => t.TradieId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(t => t.Job)
            .WithMany()
            .HasForeignKey(t => t.JobId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(t => t.Quote)
            .WithMany()
            .HasForeignKey(t => t.QuoteId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(t => t.Invoice)
            .WithMany()
            .HasForeignKey(t => t.InvoiceId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(t => t.Milestone)
            .WithMany()
            .HasForeignKey(t => t.MilestoneId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(t => t.CustomerId);
        builder.HasIndex(t => t.TradieId);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.PaidAt);
    }
}
