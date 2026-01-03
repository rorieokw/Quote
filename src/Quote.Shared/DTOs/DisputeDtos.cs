namespace Quote.Shared.DTOs;

// Main dispute DTO
public record DisputeDto(
    Guid Id,
    Guid JobId,
    string JobTitle,
    Guid JobQuoteId,
    decimal QuoteAmount,
    Guid RaisedByUserId,
    string RaisedByName,
    string RaisedByType,
    Guid? OtherPartyUserId,
    string? OtherPartyName,
    string Reason,
    string Status,
    string Description,
    string? AdminNotes,
    string? Resolution,
    string? ResolutionType,
    decimal? RefundAmount,
    string? ResolvedByName,
    DateTime CreatedAt,
    DateTime? ResolvedAt,
    List<DisputeEvidenceDto> Evidence
);

// Evidence file DTO
public record DisputeEvidenceDto(
    Guid Id,
    string FileName,
    string FileUrl,
    string? Description,
    string UploadedByName,
    DateTime UploadedAt
);

// Request to create a dispute
public record CreateDisputeRequest(
    Guid JobQuoteId,
    string Reason,
    string Description
);

// Admin request to resolve dispute
public record ResolveDisputeRequest(
    string ResolutionType,
    string Resolution,
    decimal? RefundAmount,
    string? AdminNotes
);

// Request to close/withdraw dispute
public record CloseDisputeRequest(
    string? Reason
);

// Add evidence request
public record AddDisputeEvidenceRequest(
    string FileName,
    string FileUrl,
    string? Description
);

// List response
public record DisputeListResponse(
    List<DisputeDto> Disputes,
    int TotalCount
);

// Create response
public record CreateDisputeResponse(
    Guid DisputeId
);

// Summary DTO for lists (lightweight)
public record DisputeSummaryDto(
    Guid Id,
    string JobTitle,
    string RaisedByName,
    string RaisedByType,
    string Reason,
    string Status,
    decimal QuoteAmount,
    DateTime CreatedAt
);
