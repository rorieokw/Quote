using Quote.Domain.Common;

namespace Quote.Domain.Entities;

public class TradieProfile : BaseAuditableEntity
{
    public Guid UserId { get; set; }
    public string? BusinessName { get; set; }
    public string? Bio { get; set; }
    public decimal? HourlyRate { get; set; }
    public int ServiceRadiusKm { get; set; } = 25;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool InsuranceVerified { get; set; }
    public DateTime? InsuranceExpiryDate { get; set; }
    public bool PoliceCheckVerified { get; set; }
    public bool IdentityVerified { get; set; }
    public string? AvailabilityJson { get; set; }
    public decimal? PreferredJobSizeMin { get; set; }
    public decimal? PreferredJobSizeMax { get; set; }
    public decimal Rating { get; set; }
    public int TotalJobsCompleted { get; set; }
    public int TotalReviews { get; set; }
    public double AverageResponseTimeHours { get; set; }
    public double CompletionRate { get; set; } = 1.0;

    // Available Now feature
    public bool IsAvailableNow { get; set; }
    public DateTime? AvailableNowUntil { get; set; }
    public DateTime? LastAvailableNowToggle { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<TradieLicence> Licences { get; set; } = new List<TradieLicence>();
    public ICollection<BlockedSuburb> BlockedSuburbs { get; set; } = new List<BlockedSuburb>();
    public ICollection<QuoteTemplate> QuoteTemplates { get; set; } = new List<QuoteTemplate>();
    public Subscription? Subscription { get; set; }
}
