namespace Quote.Shared.DTOs;

// Request DTOs
public record CreateRecurringJobTemplateRequest(
    Guid TradeCategoryId,
    Guid? TradieId,
    string Title,
    string? Description,
    string Address,
    string SuburbName,
    string? PostCode,
    string State,
    double? Latitude,
    double? Longitude,
    string Pattern,
    int? CustomIntervalDays,
    DateTime StartDate,
    DateTime? EndDate,
    int? MaxOccurrences,
    decimal? EstimatedBudgetMin,
    decimal? EstimatedBudgetMax,
    string? Notes,
    bool AutoAcceptFromTradie = false
);

public record UpdateRecurringJobTemplateRequest(
    string? Title,
    string? Description,
    string? Pattern,
    int? CustomIntervalDays,
    DateTime? EndDate,
    int? MaxOccurrences,
    decimal? EstimatedBudgetMin,
    decimal? EstimatedBudgetMax,
    string? Notes,
    bool? AutoAcceptFromTradie,
    bool? IsActive
);

// Response DTOs
public record RecurringJobTemplateDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    Guid? TradieId,
    string? TradieName,
    Guid TradeCategoryId,
    string TradeCategoryName,
    string Title,
    string? Description,
    string Address,
    string SuburbName,
    string? PostCode,
    string State,
    double? Latitude,
    double? Longitude,
    string Pattern,
    int CustomIntervalDays,
    DateTime StartDate,
    DateTime? EndDate,
    int? MaxOccurrences,
    int OccurrencesGenerated,
    decimal? EstimatedBudgetMin,
    decimal? EstimatedBudgetMax,
    bool IsActive,
    DateTime? LastGeneratedAt,
    DateTime? NextDueDate,
    string? Notes,
    bool AutoAcceptFromTradie,
    DateTime CreatedAt
);

public record RecurringJobTemplateListItemDto(
    Guid Id,
    string Title,
    string TradeCategoryName,
    string? TradieName,
    string Pattern,
    DateTime? NextDueDate,
    int OccurrencesGenerated,
    int? MaxOccurrences,
    bool IsActive,
    DateTime CreatedAt
);

public record RecurringJobTemplatesResponse(
    List<RecurringJobTemplateListItemDto> Templates,
    int TotalCount,
    int Page,
    int PageSize,
    RecurringJobStats Stats
);

public record RecurringJobStats(
    int TotalTemplates,
    int ActiveTemplates,
    int UpcomingThisWeek,
    int TotalJobsGenerated
);

public record GenerateJobsResult(
    int JobsGenerated,
    List<Guid> GeneratedJobIds,
    List<string> Errors
);

public record RecurringJobHistoryDto(
    Guid JobId,
    string Title,
    int RecurrenceNumber,
    string Status,
    DateTime CreatedAt,
    DateTime? CompletedAt
);

public record RecurringJobHistoryResponse(
    Guid TemplateId,
    string TemplateTitle,
    List<RecurringJobHistoryDto> Jobs,
    int TotalCount
);
