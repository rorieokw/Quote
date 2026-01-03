using Quote.Domain.Common;
using Quote.Domain.Enums;

namespace Quote.Domain.Entities;

public class Payment : BaseAuditableEntity
{
    public Guid MilestoneId { get; set; }
    public decimal Amount { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public string? StripeTransferId { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime? HeldAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public string? FailureReason { get; set; }

    // Navigation properties
    public Milestone Milestone { get; set; } = null!;
}
