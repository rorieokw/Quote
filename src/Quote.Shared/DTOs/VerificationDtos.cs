namespace Quote.Shared.DTOs;

/// <summary>
/// Request to submit identity verification (for API form binding)
/// </summary>
public class SubmitIdentityVerificationRequest
{
    public int DocumentType { get; set; }  // IdentityDocumentType enum value
    public string? DocumentNumber { get; set; }
    public string? IssuingState { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

/// <summary>
/// Request to review a verification (admin approve/reject)
/// </summary>
public record ReviewVerificationRequest
{
    public bool Approved { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Response after submitting verification documents
/// </summary>
public record SubmitVerificationResponseDto
{
    public Guid VerificationId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Pending verification item for admin queue
/// </summary>
public record PendingVerificationItemDto
{
    public Guid Id { get; init; }
    public string VerificationType { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public string TradieName { get; init; } = string.Empty;
    public string TradieEmail { get; init; } = string.Empty;
    public string? BusinessName { get; init; }
    public string DocumentType { get; init; } = string.Empty;
    public string? DocumentFrontUrl { get; init; }
    public string? DocumentBackUrl { get; init; }
    public string? DocumentNumber { get; init; }
    public string? IssuingState { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public DateTime SubmittedAt { get; init; }
    public int DaysPending { get; init; }
}

/// <summary>
/// Paginated list of pending verifications
/// </summary>
public record PendingVerificationsListDto
{
    public List<PendingVerificationItemDto> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int TotalPages { get; init; }
}

/// <summary>
/// Tradie's full verification status
/// </summary>
public record TradieVerificationStatusResponseDto
{
    public bool IdentityVerified { get; init; }
    public string? IdentityStatus { get; init; }
    public DateTime? IdentitySubmittedAt { get; init; }
    public string? IdentityNotes { get; init; }

    public bool InsuranceVerified { get; init; }
    public string? InsuranceStatus { get; init; }
    public DateTime? InsuranceSubmittedAt { get; init; }
    public DateTime? InsuranceExpiryDate { get; init; }
    public string? InsuranceNotes { get; init; }

    public bool PoliceCheckVerified { get; init; }
    public string? PoliceCheckStatus { get; init; }
    public DateTime? PoliceCheckSubmittedAt { get; init; }
    public string? PoliceCheckNotes { get; init; }

    public List<LicenceStatusDto> Licences { get; init; } = new();

    public string OverallVerificationLevel { get; init; } = "None";
    public List<string> EarnedBadges { get; init; } = new();
}

/// <summary>
/// Licence verification status
/// </summary>
public record LicenceStatusDto
{
    public Guid Id { get; init; }
    public string TradeCategory { get; init; } = string.Empty;
    public string LicenceNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime? VerifiedAt { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Response after admin reviews verification
/// </summary>
public record ReviewVerificationResponseDto
{
    public Guid VerificationId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string TradieName { get; init; } = string.Empty;
}
