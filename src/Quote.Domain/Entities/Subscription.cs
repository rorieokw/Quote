using Quote.Domain.Common;
using Quote.Domain.Enums;

namespace Quote.Domain.Entities;

public class Subscription : BaseAuditableEntity
{
    public Guid TradieProfileId { get; set; }
    public SubscriptionTier Tier { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? StripeCustomerId { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public int JobViewsThisPeriod { get; set; }
    public DateTime? CancelledAt { get; set; }

    // Navigation properties
    public TradieProfile TradieProfile { get; set; } = null!;

    public int MaxJobViews => Tier switch
    {
        SubscriptionTier.Starter => 10,
        SubscriptionTier.Professional => int.MaxValue,
        SubscriptionTier.Business => int.MaxValue,
        _ => 0
    };

    public bool CanViewJobs => Status == SubscriptionStatus.Active && JobViewsThisPeriod < MaxJobViews;
}
