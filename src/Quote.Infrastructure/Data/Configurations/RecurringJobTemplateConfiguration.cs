using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quote.Domain.Entities;

namespace Quote.Infrastructure.Data.Configurations;

public class RecurringJobTemplateConfiguration : IEntityTypeConfiguration<RecurringJobTemplate>
{
    public void Configure(EntityTypeBuilder<RecurringJobTemplate> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Description)
            .HasMaxLength(2000);

        builder.Property(r => r.Address)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(r => r.SuburbName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.PostCode)
            .HasMaxLength(10);

        builder.Property(r => r.State)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(r => r.EstimatedBudgetMin)
            .HasPrecision(18, 2);

        builder.Property(r => r.EstimatedBudgetMax)
            .HasPrecision(18, 2);

        builder.Property(r => r.Notes)
            .HasMaxLength(1000);

        builder.HasOne(r => r.Customer)
            .WithMany()
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Tradie)
            .WithMany()
            .HasForeignKey(r => r.TradieId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.TradeCategory)
            .WithMany()
            .HasForeignKey(r => r.TradeCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.GeneratedJobs)
            .WithOne(j => j.RecurringTemplate)
            .HasForeignKey(j => j.RecurringTemplateId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => r.CustomerId);
        builder.HasIndex(r => r.TradieId);
        builder.HasIndex(r => r.IsActive);
        builder.HasIndex(r => r.NextDueDate);
    }
}
