using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quote.Domain.Entities;

namespace Quote.Infrastructure.Data.Configurations;

public class CustomerQualityConfiguration : IEntityTypeConfiguration<CustomerQuality>
{
    public void Configure(EntityTypeBuilder<CustomerQuality> builder)
    {
        builder.HasKey(cq => cq.Id);

        builder.Property(cq => cq.AverageJobValue)
            .HasPrecision(18, 2);

        builder.Property(cq => cq.PaymentReliabilityScore)
            .HasPrecision(5, 2);

        builder.Property(cq => cq.AverageRatingGiven)
            .HasPrecision(3, 2);

        builder.HasOne(cq => cq.Customer)
            .WithOne()
            .HasForeignKey<CustomerQuality>(cq => cq.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(cq => cq.CustomerId).IsUnique();
    }
}
