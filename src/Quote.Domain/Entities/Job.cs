using Quote.Domain.Common;
using Quote.Domain.Enums;

namespace Quote.Domain.Entities;

public class Job : BaseAuditableEntity
{
    public Guid CustomerId { get; set; }
    public Guid TradeCategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public JobStatus Status { get; set; } = JobStatus.Draft;
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public DateTime? PreferredStartDate { get; set; }
    public DateTime? PreferredEndDate { get; set; }
    public bool IsFlexibleDates { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string SuburbName { get; set; } = string.Empty;
    public AustralianState State { get; set; }
    public string Postcode { get; set; } = string.Empty;
    public PropertyType PropertyType { get; set; }
    public Guid? AcceptedQuoteId { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Recurring job support
    public Guid? RecurringTemplateId { get; set; }
    public int? RecurrenceNumber { get; set; }

    // Lead priority (premium tradies see jobs first)
    public DateTime? PremiumVisibleFrom { get; set; }
    public DateTime? PublicVisibleFrom { get; set; }

    // Navigation properties
    public User Customer { get; set; } = null!;
    public TradeCategory TradeCategory { get; set; } = null!;
    public JobQuote? AcceptedQuote { get; set; }
    public ICollection<JobMedia> Media { get; set; } = new List<JobMedia>();
    public ICollection<JobQuote> Quotes { get; set; } = new List<JobQuote>();
    public ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public Conversation? Conversation { get; set; }
    public RecurringJobTemplate? RecurringTemplate { get; set; }
}
