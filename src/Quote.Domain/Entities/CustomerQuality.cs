using Quote.Domain.Common;

namespace Quote.Domain.Entities;

public class CustomerQuality : BaseEntity
{
    public Guid CustomerId { get; set; }
    public int TotalJobsPosted { get; set; }
    public int JobsCompleted { get; set; }
    public int JobsCancelled { get; set; }
    public decimal AverageJobValue { get; set; }
    public decimal PaymentReliabilityScore { get; set; }
    public double AverageResponseTimeHours { get; set; }
    public int TotalReviewsGiven { get; set; }
    public decimal AverageRatingGiven { get; set; }
    public DateTime LastCalculatedAt { get; set; }

    // Navigation properties
    public User Customer { get; set; } = null!;
}
