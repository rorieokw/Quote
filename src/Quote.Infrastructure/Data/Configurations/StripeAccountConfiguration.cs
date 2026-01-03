using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quote.Domain.Entities;

namespace Quote.Infrastructure.Data.Configurations;

public class StripeAccountConfiguration : IEntityTypeConfiguration<StripeAccount>
{
    public void Configure(EntityTypeBuilder<StripeAccount> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.StripeAccountId)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(s => s.StripeAccountId)
            .IsUnique();

        builder.HasIndex(s => s.TradieId)
            .IsUnique();

        builder.Property(s => s.DefaultCurrency)
            .HasMaxLength(10);

        builder.Property(s => s.Country)
            .HasMaxLength(10);

        builder.Property(s => s.BusinessType)
            .HasMaxLength(50);

        builder.Property(s => s.Email)
            .HasMaxLength(255);

        builder.HasOne(s => s.Tradie)
            .WithOne()
            .HasForeignKey<StripeAccount>(s => s.TradieId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Payouts)
            .WithOne(p => p.StripeAccount)
            .HasForeignKey(p => p.StripeAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
