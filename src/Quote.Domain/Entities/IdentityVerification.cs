using Quote.Domain.Common;
using Quote.Domain.Enums;

namespace Quote.Domain.Entities;

public class IdentityVerification : BaseAuditableEntity
{
    public Guid UserId { get; set; }
    public IdentityDocumentType DocumentType { get; set; }
    public string? DocumentFrontUrl { get; set; }
    public string? DocumentBackUrl { get; set; }
    public string? DocumentNumber { get; set; }
    public string? IssuingState { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
    public DateTime? VerifiedAt { get; set; }
    public string? VerificationNotes { get; set; }
    public Guid? ReviewedByUserId { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public User? ReviewedBy { get; set; }
}
