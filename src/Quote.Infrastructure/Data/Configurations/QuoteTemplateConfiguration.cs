using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quote.Domain.Entities;

namespace Quote.Infrastructure.Data.Configurations;

public class QuoteTemplateConfiguration : IEntityTypeConfiguration<QuoteTemplate>
{
    public void Configure(EntityTypeBuilder<QuoteTemplate> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.Property(t => t.DefaultLabourCost)
            .HasPrecision(18, 2);

        builder.Property(t => t.DefaultMaterialsCost)
            .HasPrecision(18, 2);

        builder.Property(t => t.DefaultNotes)
            .HasMaxLength(2000);

        builder.HasOne(t => t.Tradie)
            .WithMany()
            .HasForeignKey(t => t.TradieId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.TradeCategory)
            .WithMany()
            .HasForeignKey(t => t.TradeCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.TradieId);
        builder.HasIndex(t => t.TradeCategoryId);
    }
}

public class QuoteTemplateMaterialConfiguration : IEntityTypeConfiguration<QuoteTemplateMaterial>
{
    public void Configure(EntityTypeBuilder<QuoteTemplateMaterial> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Unit)
            .HasMaxLength(50);

        builder.Property(m => m.Quantity)
            .HasPrecision(18, 4);

        builder.Property(m => m.EstimatedUnitPrice)
            .HasPrecision(18, 2);

        builder.HasOne(m => m.Template)
            .WithMany(t => t.Materials)
            .HasForeignKey(m => m.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(m => m.TotalPrice);
    }
}
