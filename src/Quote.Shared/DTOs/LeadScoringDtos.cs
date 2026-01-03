namespace Quote.Shared.DTOs;

public record ScoredLeadDto(
    Guid JobId,
    string Title,
    string Description,
    string TradeCategory,
    string TradeCategoryIcon,
    decimal? BudgetMin,
    decimal? BudgetMax,
    string SuburbName,
    string State,
    string Postcode,
    DateTime? PreferredStartDate,
    bool IsFlexibleDates,
    string CustomerName,
    int QuoteCount,
    DateTime CreatedAt,
    List<string> MediaUrls,
    // Scoring details
    int TotalScore,
    int DistanceScore,
    int BudgetMatchScore,
    int SkillMatchScore,
    int CustomerQualityScore,
    int UrgencyScore,
    double DistanceKm,
    string ScoreRating, // "Excellent", "Good", "Fair", "Low"
    LeadCustomerQualityDto? CustomerQuality
);

public record LeadCustomerQualityDto(
    int TotalJobsPosted,
    int JobsCompleted,
    int JobsCancelled,
    decimal CompletionRate,
    decimal PaymentReliabilityScore,
    double AverageResponseTimeHours,
    string QualityRating // "Excellent", "Good", "Fair", "New"
);

public record ScoredLeadsResponse(
    List<ScoredLeadDto> Leads,
    int TotalCount,
    int Page,
    int PageSize,
    LeadScoringStats Stats
);

public record LeadScoringStats(
    int ExcellentLeads,
    int GoodLeads,
    int FairLeads,
    int LowLeads,
    double AverageScore
);

public record LeadScoreDetailDto(
    Guid JobId,
    int TotalScore,
    int DistanceScore,
    int BudgetMatchScore,
    int SkillMatchScore,
    int CustomerQualityScore,
    int UrgencyScore,
    double DistanceKm,
    string ScoreRating,
    List<string> ScoreExplanations
);
