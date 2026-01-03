namespace Quote.Shared.DTOs;

public record JobDto(
    Guid Id,
    string Title,
    string Description,
    string Status,
    string TradeCategory,
    decimal? BudgetMin,
    decimal? BudgetMax,
    string SuburbName,
    string State,
    string Postcode,
    DateTime? PreferredStartDate,
    DateTime CreatedAt,
    string CustomerName,
    int QuoteCount,
    double DistanceKm = 0,
    List<string>? MediaUrls = null,
    Guid? TradeCategoryId = null
)
{
    // Aliases for backward compatibility with UI components
    public string TradeCategoryName => TradeCategory;
    public string? TradeCategoryIcon => null; // Not returned by API
    public bool IsFlexibleDates => false; // Not returned by list endpoint
}

public record CreateJobRequest(
    Guid TradeCategoryId,
    string Title,
    string Description,
    decimal? BudgetMin,
    decimal? BudgetMax,
    DateTime? PreferredStartDate,
    DateTime? PreferredEndDate,
    bool IsFlexibleDates,
    double Latitude,
    double Longitude,
    string SuburbName,
    string State,
    string Postcode,
    string PropertyType
);

public record JobListResponse(
    List<JobDto> Items,
    int TotalCount,
    int PageNumber,
    int TotalPages
)
{
    // Alias for easier access in components
    public List<JobDto> Jobs => Items;
    public int Page => PageNumber;
}
