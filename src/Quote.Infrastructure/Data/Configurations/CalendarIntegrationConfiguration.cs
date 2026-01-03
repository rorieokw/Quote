using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quote.Domain.Entities;

namespace Quote.Infrastructure.Data.Configurations;

public class CalendarIntegrationConfiguration : IEntityTypeConfiguration<CalendarIntegration>
{
    public void Configure(EntityTypeBuilder<CalendarIntegration> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Provider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.EncryptedRefreshToken)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(c => c.CalendarId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.LastSyncError)
            .HasMaxLength(1000);

        builder.HasOne(c => c.TradieProfile)
            .WithMany()
            .HasForeignKey(c => c.TradieProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.TradieProfileId).IsUnique();
    }
}
