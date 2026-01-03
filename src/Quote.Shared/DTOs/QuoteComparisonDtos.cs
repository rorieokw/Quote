namespace Quote.Shared.DTOs;

public record QuoteComparisonDto(
    Guid QuoteId,
    Guid TradieId,
    string TradieName,
    string? TradieBusinessName,
    string? TradiePhotoUrl,
    decimal TradieRating,
    int TradieReviewCount,
    int TradieJobsCompleted,
    double TradieCompletionRate,
    double TradieResponseTimeHours,
    VerificationBadgesDto Verification,
    decimal LabourCost,
    decimal MaterialsCost,
    decimal TotalCost,
    int EstimatedHours,
    DateTime? ProposedStartDate,
    string? Notes,
    bool DepositRequired,
    decimal? DepositAmount,
    DateTime CreatedAt,
    DateTime ValidUntil,
    string PriceRating,
    decimal? MarketComparison
);

public record VerificationBadgesDto(
    bool IsLicensed,
    bool IsInsured,
    bool IsPoliceChecked,
    bool IsIdentityVerified,
    string VerificationLevel
);

public record QuoteComparisonResponse(
    Guid JobId,
    string JobTitle,
    string TradeCategory,
    decimal? BudgetMin,
    decimal? BudgetMax,
    List<QuoteComparisonDto> Quotes,
    int TotalQuotes,
    decimal LowestPrice,
    decimal HighestPrice,
    decimal AveragePrice,
    PriceBenchmarkDto? MarketBenchmark
);
