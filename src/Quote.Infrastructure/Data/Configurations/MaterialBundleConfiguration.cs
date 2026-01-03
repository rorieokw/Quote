using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quote.Domain.Entities;

namespace Quote.Infrastructure.Data.Configurations;

public class MaterialBundleConfiguration : IEntityTypeConfiguration<MaterialBundle>
{
    public void Configure(EntityTypeBuilder<MaterialBundle> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.Description)
            .HasMaxLength(500);

        builder.HasOne(b => b.Tradie)
            .WithMany()
            .HasForeignKey(b => b.TradieId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.TradeCategory)
            .WithMany()
            .HasForeignKey(b => b.TradeCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => b.TradieId);
        builder.HasIndex(b => b.TradeCategoryId);
    }
}

public class MaterialBundleItemConfiguration : IEntityTypeConfiguration<MaterialBundleItem>
{
    public void Configure(EntityTypeBuilder<MaterialBundleItem> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.SupplierName)
            .HasMaxLength(200);

        builder.Property(i => i.ProductUrl)
            .HasMaxLength(500);

        builder.Property(i => i.Unit)
            .HasMaxLength(50);

        builder.Property(i => i.DefaultQuantity)
            .HasPrecision(18, 4);

        builder.Property(i => i.EstimatedUnitPrice)
            .HasPrecision(18, 2);

        builder.HasOne(i => i.Bundle)
            .WithMany(b => b.Items)
            .HasForeignKey(i => i.BundleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(i => i.TotalPrice);
    }
}
