using Quote.Domain.Common;

namespace Quote.Domain.Entities;

public class LeadScore : BaseEntity
{
    public Guid JobId { get; set; }
    public Guid TradieId { get; set; }
    public int TotalScore { get; set; }
    public int DistanceScore { get; set; }
    public int BudgetMatchScore { get; set; }
    public int SkillMatchScore { get; set; }
    public int CustomerQualityScore { get; set; }
    public int UrgencyScore { get; set; }
    public double DistanceKm { get; set; }
    public DateTime CalculatedAt { get; set; }

    // Navigation properties
    public Job Job { get; set; } = null!;
    public User Tradie { get; set; } = null!;
}
