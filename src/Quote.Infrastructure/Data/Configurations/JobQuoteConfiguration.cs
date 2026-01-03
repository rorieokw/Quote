using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quote.Domain.Entities;

namespace Quote.Infrastructure.Data.Configurations;

public class JobQuoteConfiguration : IEntityTypeConfiguration<JobQuote>
{
    public void Configure(EntityTypeBuilder<JobQuote> builder)
    {
        builder.HasKey(q => q.Id);

        builder.Property(q => q.LabourCost)
            .HasPrecision(18, 2);

        builder.Property(q => q.MaterialsCost)
            .HasPrecision(18, 2);

        builder.Property(q => q.Notes)
            .HasMaxLength(2000);

        builder.Ignore(q => q.TotalCost);

        builder.HasOne(q => q.Job)
            .WithMany(j => j.Quotes)
            .HasForeignKey(q => q.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(q => q.Tradie)
            .WithMany(u => u.TradieQuotes)
            .HasForeignKey(q => q.TradieId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(q => new { q.JobId, q.TradieId })
            .IsUnique();
    }
}
