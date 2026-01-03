using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quote.Domain.Entities;

namespace Quote.Infrastructure.Data.Configurations;

public class LeadScoreConfiguration : IEntityTypeConfiguration<LeadScore>
{
    public void Configure(EntityTypeBuilder<LeadScore> builder)
    {
        builder.HasKey(ls => ls.Id);

        builder.HasOne(ls => ls.Job)
            .WithMany()
            .HasForeignKey(ls => ls.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ls => ls.Tradie)
            .WithMany()
            .HasForeignKey(ls => ls.TradieId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ls => new { ls.TradieId, ls.JobId }).IsUnique();
        builder.HasIndex(ls => ls.TotalScore);
        builder.HasIndex(ls => ls.CalculatedAt);
    }
}
