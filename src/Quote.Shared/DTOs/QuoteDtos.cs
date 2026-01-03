namespace Quote.Shared.DTOs;

// Quick Quote
public record QuickQuoteRequest(
    Guid JobId,
    decimal LabourCost,
    decimal? MaterialsCost,
    int EstimatedDurationHours,
    string? Notes,
    Guid? TemplateId,
    DateTime? ProposedStartDate,
    bool DepositRequired = false,
    decimal? DepositPercentage = null,
    // Material line items (optional - auto-calculates MaterialsCost if provided)
    List<QuoteMaterialRequest>? Materials = null,
    // Load materials from saved bundle
    Guid? MaterialBundleId = null,
    // Save inline materials as a new bundle
    bool SaveMaterialsAsBundle = false,
    string? NewBundleName = null
);

public record QuickQuoteResponse(
    Guid QuoteId,
    Guid JobId,
    decimal TotalCost,
    string Status,
    DateTime ValidUntil
);

// Quote Details
public record QuoteDto(
    Guid Id,
    Guid JobId,
    string JobTitle,
    Guid TradieId,
    string TradieName,
    string? TradieBusinessName,
    decimal LabourCost,
    decimal MaterialsCost,
    decimal TotalCost,
    int EstimatedDurationHours,
    DateTime? ProposedStartDate,
    string? Notes,
    string Status,
    DateTime ValidUntil,
    DateTime CreatedAt,
    int ViewCount,
    DateTime? FirstViewedAt,
    DateTime? LastViewedAt,
    bool DepositRequired,
    decimal? RequiredDepositAmount
);

// Quote with tracking status (for tradie's "My Quotes" page)
public record QuoteStatusDto(
    Guid QuoteId,
    Guid JobId,
    string JobTitle,
    string JobSuburb,
    string CustomerName,
    string Status,
    decimal TotalCost,
    int ViewCount,
    DateTime? FirstViewedAt,
    DateTime? LastViewedAt,
    int CompetingQuotes,
    bool IsWinningQuote,
    bool IsJobStillOpen,
    DateTime CreatedAt,
    DateTime ValidUntil
);

public record QuoteListResponse(
    List<QuoteStatusDto> Quotes,
    int TotalCount,
    int Page,
    int PageSize
);
