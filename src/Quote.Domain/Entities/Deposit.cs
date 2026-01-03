using Quote.Domain.Common;
using Quote.Domain.Enums;

namespace Quote.Domain.Entities;

public class Deposit : BaseAuditableEntity
{
    public Guid JobQuoteId { get; set; }
    public decimal Amount { get; set; }
    public decimal PercentageOfTotal { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime? PaidAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public string? RefundReason { get; set; }

    // Navigation properties
    public JobQuote Quote { get; set; } = null!;
}
