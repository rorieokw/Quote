namespace Quote.Shared.DTOs;

public record PriceBenchmarkDto(
    Guid TradeCategoryId,
    string TradeCategoryName,
    string? Location,
    decimal MinPrice,
    decimal MaxPrice,
    decimal AveragePrice,
    decimal MedianPrice,
    int SampleSize,
    DateTime LastUpdated
);

public record QuotePriceComparisonDto(
    Guid QuoteId,
    decimal YourPrice,
    decimal MarketAverage,
    decimal PercentageDifference,
    string Rating,
    string Explanation,
    PriceBenchmarkDto? Benchmark
);

public record PriceBenchmarkResponse(
    List<PriceBenchmarkDto> Benchmarks,
    int TotalCount
);

public record TradieQuotesComparisonResponse(
    List<QuotePriceComparisonDto> Quotes,
    decimal AveragePosition,
    int BelowMarketCount,
    int AtMarketCount,
    int AboveMarketCount
);

public record GetPriceBenchmarkRequest(
    Guid TradeCategoryId,
    string? Postcode = null
);
