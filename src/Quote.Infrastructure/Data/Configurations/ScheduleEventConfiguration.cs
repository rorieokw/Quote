using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quote.Domain.Entities;

namespace Quote.Infrastructure.Data.Configurations;

public class ScheduleEventConfiguration : IEntityTypeConfiguration<ScheduleEvent>
{
    public void Configure(EntityTypeBuilder<ScheduleEvent> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.EventType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.Address)
            .HasMaxLength(500);

        builder.Property(e => e.SuburbName)
            .HasMaxLength(100);

        builder.Property(e => e.Color)
            .HasMaxLength(20);

        builder.Property(e => e.Notes)
            .HasMaxLength(2000);

        builder.Property(e => e.RecurrenceRule)
            .HasMaxLength(500);

        builder.HasOne(e => e.Tradie)
            .WithMany()
            .HasForeignKey(e => e.TradieId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Job)
            .WithMany()
            .HasForeignKey(e => e.JobId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Quote)
            .WithMany()
            .HasForeignKey(e => e.QuoteId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.PreviousEvent)
            .WithMany()
            .HasForeignKey(e => e.PreviousEventId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(e => e.TradieId);
        builder.HasIndex(e => e.StartTime);
        builder.HasIndex(e => e.EndTime);
        builder.HasIndex(e => new { e.TradieId, e.StartTime, e.EndTime });
        builder.HasIndex(e => e.JobId);
        builder.HasIndex(e => e.RecurrenceGroupId);
    }
}
