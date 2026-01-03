using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quote.Domain.Entities;

namespace Quote.Infrastructure.Data.Configurations;

public class QuoteViewConfiguration : IEntityTypeConfiguration<QuoteView>
{
    public void Configure(EntityTypeBuilder<QuoteView> builder)
    {
        builder.HasKey(v => v.Id);

        builder.HasOne(v => v.Quote)
            .WithMany(q => q.Views)
            .HasForeignKey(v => v.QuoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.ViewedBy)
            .WithMany()
            .HasForeignKey(v => v.ViewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => v.QuoteId);
        builder.HasIndex(v => v.ViewedAt);
    }
}
