using Quote.Shared.DTOs;

namespace Quote.Application.PriceBenchmarking.Services;

public interface IPriceBenchmarkingService
{
    Task<PriceBenchmarkDto?> GetBenchmarkAsync(Guid tradeCategoryId, string? postcode = null, CancellationToken cancellationToken = default);
    Task<QuotePriceComparisonDto?> CompareQuotePriceAsync(Guid quoteId, CancellationToken cancellationToken = default);
    Task<TradieQuotesComparisonResponse> GetTradieQuotesComparisonAsync(Guid tradieId, CancellationToken cancellationToken = default);
    decimal CalculatePercentageDifference(decimal yourPrice, decimal marketAverage);
    string GetPriceRating(decimal percentageDifference);
    string GetPriceExplanation(decimal percentageDifference, decimal marketAverage, string tradeCategoryName);
}
