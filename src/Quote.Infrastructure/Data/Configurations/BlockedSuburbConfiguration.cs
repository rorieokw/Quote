using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quote.Domain.Entities;

namespace Quote.Infrastructure.Data.Configurations;

public class BlockedSuburbConfiguration : IEntityTypeConfiguration<BlockedSuburb>
{
    public void Configure(EntityTypeBuilder<BlockedSuburb> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.SuburbName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.Postcode)
            .IsRequired()
            .HasMaxLength(4);

        builder.Property(b => b.Reason)
            .HasMaxLength(200);

        builder.HasOne(b => b.TradieProfile)
            .WithMany(t => t.BlockedSuburbs)
            .HasForeignKey(b => b.TradieProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => b.TradieProfileId);
        builder.HasIndex(b => b.Postcode);
    }
}
