using Quote.Domain.Common;
using Quote.Domain.Enums;

namespace Quote.Domain.Entities;

public class JobQuote : BaseAuditableEntity
{
    public Guid JobId { get; set; }
    public Guid TradieId { get; set; }
    public QuoteStatus Status { get; set; } = QuoteStatus.Pending;
    public decimal LabourCost { get; set; }
    public decimal MaterialsCost { get; set; }
    public decimal TotalCost => LabourCost + MaterialsCost;
    public int EstimatedDurationHours { get; set; }
    public DateTime? ProposedStartDate { get; set; }
    public string? Notes { get; set; }
    public DateTime ValidUntil { get; set; }

    // Template reference (for quick quotes)
    public Guid? TemplateId { get; set; }

    // Response tracking
    public int ViewCount { get; set; }
    public DateTime? FirstViewedAt { get; set; }
    public DateTime? LastViewedAt { get; set; }

    // Deposit requirements
    public bool DepositRequired { get; set; }
    public decimal? RequiredDepositAmount { get; set; }
    public decimal? RequiredDepositPercentage { get; set; }

    // Navigation properties
    public Job Job { get; set; } = null!;
    public User Tradie { get; set; } = null!;
    public QuoteTemplate? Template { get; set; }
    public ICollection<QuoteMaterial> Materials { get; set; } = new List<QuoteMaterial>();
    public ICollection<QuoteView> Views { get; set; } = new List<QuoteView>();
}
