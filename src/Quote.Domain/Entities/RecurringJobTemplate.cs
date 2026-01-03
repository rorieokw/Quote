using Quote.Domain.Common;
using Quote.Domain.Enums;

namespace Quote.Domain.Entities;

public class RecurringJobTemplate : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Guid? TradieId { get; set; }
    public Guid TradeCategoryId { get; set; }

    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string Address { get; set; } = null!;
    public string SuburbName { get; set; } = null!;
    public string? PostCode { get; set; }
    public string State { get; set; } = null!;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public RecurrencePattern Pattern { get; set; }
    public int CustomIntervalDays { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? MaxOccurrences { get; set; }
    public int OccurrencesGenerated { get; set; }

    public decimal? EstimatedBudgetMin { get; set; }
    public decimal? EstimatedBudgetMax { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime? LastGeneratedAt { get; set; }
    public DateTime? NextDueDate { get; set; }

    public string? Notes { get; set; }
    public bool AutoAcceptFromTradie { get; set; }

    // Navigation
    public User Customer { get; set; } = null!;
    public User? Tradie { get; set; }
    public TradeCategory TradeCategory { get; set; } = null!;
    public ICollection<Job> GeneratedJobs { get; set; } = new List<Job>();
}
