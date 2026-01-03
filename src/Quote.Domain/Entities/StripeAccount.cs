using Quote.Domain.Common;

namespace Quote.Domain.Entities;

public class StripeAccount : BaseEntity
{
    public Guid TradieId { get; set; }
    public string StripeAccountId { get; set; } = null!;
    public bool IsOnboardingComplete { get; set; }
    public bool ChargesEnabled { get; set; }
    public bool PayoutsEnabled { get; set; }
    public bool DetailsSubmitted { get; set; }
    public string? DefaultCurrency { get; set; }
    public string? Country { get; set; }
    public string? BusinessType { get; set; }
    public string? Email { get; set; }
    public DateTime? OnboardingCompletedAt { get; set; }

    // Navigation
    public User Tradie { get; set; } = null!;
    public ICollection<Payout> Payouts { get; set; } = new List<Payout>();
}
