using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quote.Domain.Entities;

namespace Quote.Infrastructure.Data.Configurations;

public class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Email)
            .HasMaxLength(256);

        builder.Property(t => t.Phone)
            .HasMaxLength(20);

        builder.Property(t => t.HourlyRate)
            .HasPrecision(18, 2);

        builder.HasOne(t => t.TradieProfile)
            .WithMany()
            .HasForeignKey(t => t.TradieProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(t => t.FullName);

        builder.HasIndex(t => t.TradieProfileId);
        builder.HasIndex(t => t.IsActive);
    }
}

public class JobAssignmentConfiguration : IEntityTypeConfiguration<JobAssignment>
{
    public void Configure(EntityTypeBuilder<JobAssignment> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Notes)
            .HasMaxLength(500);

        builder.HasOne(a => a.JobQuote)
            .WithMany()
            .HasForeignKey(a => a.JobQuoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.TeamMember)
            .WithMany(t => t.Assignments)
            .HasForeignKey(a => a.TeamMemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.ScheduledDate);
        builder.HasIndex(a => a.TeamMemberId);
    }
}
