using Quote.Domain.Common;
using Quote.Domain.Enums;

namespace Quote.Domain.Entities;

public class Payout : BaseEntity
{
    public Guid StripeAccountId { get; set; }
    public Guid TradieId { get; set; }
    public string StripePayoutId { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "aud";
    public PayoutStatus Status { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public DateTime? ArrivalDate { get; set; }
    public string? Description { get; set; }
    public string? StatementDescriptor { get; set; }

    // Navigation
    public StripeAccount StripeAccount { get; set; } = null!;
    public User Tradie { get; set; } = null!;
}
