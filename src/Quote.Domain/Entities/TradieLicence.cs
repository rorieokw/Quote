using Quote.Domain.Common;
using Quote.Domain.Enums;

namespace Quote.Domain.Entities;

public class TradieLicence : BaseAuditableEntity
{
    public Guid TradieProfileId { get; set; }
    public Guid TradeCategoryId { get; set; }
    public string LicenceNumber { get; set; } = string.Empty;
    public AustralianState LicenceState { get; set; }
    public string? IssuingAuthority { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
    public DateTime? VerifiedAt { get; set; }
    public string? VerificationNotes { get; set; }
    public string? DocumentUrl { get; set; }

    // Navigation properties
    public TradieProfile TradieProfile { get; set; } = null!;
    public TradeCategory TradeCategory { get; set; } = null!;
}
