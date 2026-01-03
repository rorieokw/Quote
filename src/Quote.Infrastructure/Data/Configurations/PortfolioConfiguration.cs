using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quote.Domain.Entities;

namespace Quote.Infrastructure.Data.Configurations;

public class PortfolioItemConfiguration : IEntityTypeConfiguration<PortfolioItem>
{
    public void Configure(EntityTypeBuilder<PortfolioItem> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        builder.HasOne(p => p.TradieProfile)
            .WithMany()
            .HasForeignKey(p => p.TradieProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.CompletedJob)
            .WithMany()
            .HasForeignKey(p => p.CompletedJobId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(p => p.TradeCategory)
            .WithMany()
            .HasForeignKey(p => p.TradeCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.TradieProfileId);
        builder.HasIndex(p => p.IsPublic);
        builder.HasIndex(p => p.IsFeatured);
    }
}

public class PortfolioMediaConfiguration : IEntityTypeConfiguration<PortfolioMedia>
{
    public void Configure(EntityTypeBuilder<PortfolioMedia> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.MediaUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(m => m.Caption)
            .HasMaxLength(500);

        builder.HasOne(m => m.PortfolioItem)
            .WithMany(p => p.Media)
            .HasForeignKey(m => m.PortfolioItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
