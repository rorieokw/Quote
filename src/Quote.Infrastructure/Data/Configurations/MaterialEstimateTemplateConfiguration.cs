using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quote.Domain.Entities;

namespace Quote.Infrastructure.Data.Configurations;

public class MaterialEstimateTemplateConfiguration : IEntityTypeConfiguration<MaterialEstimateTemplate>
{
    public void Configure(EntityTypeBuilder<MaterialEstimateTemplate> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Description)
            .HasMaxLength(1000);

        builder.Property(m => m.CalculationFormulaJson)
            .IsRequired()
            .HasMaxLength(10000);

        builder.HasOne(m => m.TradeCategory)
            .WithMany()
            .HasForeignKey(m => m.TradeCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.CreatedByTradie)
            .WithMany()
            .HasForeignKey(m => m.CreatedByTradieId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(m => m.TradeCategoryId);
        builder.HasIndex(m => m.IsSystemTemplate);
    }
}
