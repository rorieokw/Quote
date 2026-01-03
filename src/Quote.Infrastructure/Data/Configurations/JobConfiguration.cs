using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quote.Domain.Entities;

namespace Quote.Infrastructure.Data.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.HasKey(j => j.Id);

        builder.Property(j => j.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(j => j.Description)
            .IsRequired()
            .HasMaxLength(5000);

        builder.Property(j => j.SuburbName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(j => j.Postcode)
            .IsRequired()
            .HasMaxLength(4);

        builder.Property(j => j.BudgetMin)
            .HasPrecision(18, 2);

        builder.Property(j => j.BudgetMax)
            .HasPrecision(18, 2);

        builder.HasOne(j => j.Customer)
            .WithMany(u => u.CustomerJobs)
            .HasForeignKey(j => j.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(j => j.TradeCategory)
            .WithMany(t => t.Jobs)
            .HasForeignKey(j => j.TradeCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(j => j.AcceptedQuote)
            .WithOne()
            .HasForeignKey<Job>(j => j.AcceptedQuoteId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(j => j.Status);
        builder.HasIndex(j => j.State);
        builder.HasIndex(j => j.Postcode);
        builder.HasIndex(j => new { j.Latitude, j.Longitude });
    }
}
