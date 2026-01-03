using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quote.Domain.Entities;

namespace Quote.Infrastructure.Data.Configurations;

public class PhotoAnnotationConfiguration : IEntityTypeConfiguration<PhotoAnnotation>
{
    public void Configure(EntityTypeBuilder<PhotoAnnotation> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AnnotatedImageUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.AnnotationJson)
            .HasMaxLength(10000);

        builder.HasOne(a => a.Quote)
            .WithMany()
            .HasForeignKey(a => a.QuoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.OriginalMedia)
            .WithMany()
            .HasForeignKey(a => a.OriginalMediaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.QuoteId);
    }
}
