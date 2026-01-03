using Quote.Domain.Common;

namespace Quote.Domain.Entities;

public class PaymentTransaction : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Guid? TradieId { get; set; }
    public Guid? JobId { get; set; }
    public Guid? QuoteId { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid? MilestoneId { get; set; }

    public string StripePaymentIntentId { get; set; } = null!;
    public string? StripeChargeId { get; set; }
    public decimal Amount { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal TradiePayout { get; set; }
    public string Currency { get; set; } = "aud";
    public string Status { get; set; } = null!; // requires_payment_method, requires_confirmation, requires_action, processing, succeeded, canceled
    public string? PaymentMethodType { get; set; }
    public string? ReceiptEmail { get; set; }
    public string? Description { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public bool IsDeposit { get; set; }
    public bool IsMilestonePayment { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public decimal? RefundedAmount { get; set; }

    // Navigation
    public User Customer { get; set; } = null!;
    public User? Tradie { get; set; }
    public Job? Job { get; set; }
    public JobQuote? Quote { get; set; }
    public Invoice? Invoice { get; set; }
    public Milestone? Milestone { get; set; }
}
