using Quote.Domain.Common;
using Quote.Domain.Enums;

namespace Quote.Domain.Entities;

public class Dispute : BaseAuditableEntity
{
    public Guid JobId { get; set; }
    public Guid JobQuoteId { get; set; }
    public Guid RaisedByUserId { get; set; }
    public DisputeReason Reason { get; set; }
    public DisputeStatus Status { get; set; } = DisputeStatus.Open;
    public string Description { get; set; } = string.Empty;

    // Resolution fields
    public string? AdminNotes { get; set; }
    public string? Resolution { get; set; }
    public DisputeResolutionType? ResolutionType { get; set; }
    public decimal? RefundAmount { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public DateTime? ResolvedAt { get; set; }

    // Navigation properties
    public Job Job { get; set; } = null!;
    public JobQuote JobQuote { get; set; } = null!;
    public User RaisedByUser { get; set; } = null!;
    public User? ResolvedByUser { get; set; }
    public ICollection<DisputeEvidence> Evidence { get; set; } = new List<DisputeEvidence>();
}
