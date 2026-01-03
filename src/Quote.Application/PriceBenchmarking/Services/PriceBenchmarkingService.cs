using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Domain.Enums;
using Quote.Shared.DTOs;

namespace Quote.Application.PriceBenchmarking.Services;

public class PriceBenchmarkingService : IPriceBenchmarkingService
{
    private readonly IApplicationDbContext _context;
    private const int MinimumSampleSize = 5;

    public PriceBenchmarkingService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PriceBenchmarkDto?> GetBenchmarkAsync(Guid tradeCategoryId, string? postcode = null, CancellationToken cancellationToken = default)
    {
        var category = await _context.TradeCategories
            .FirstOrDefaultAsync(c => c.Id == tradeCategoryId, cancellationToken);

        if (category == null) return null;

        // Build query for relevant quotes
        var query = _context.Quotes
            .Include(q => q.Job)
            .Where(q => q.Job.TradeCategoryId == tradeCategoryId)
            .Where(q => q.Status == QuoteStatus.Accepted || q.Job.Status == JobStatus.Completed)
            .Where(q => q.CreatedAt >= DateTime.UtcNow.AddMonths(-6)); // Recent data only

        // Filter by postcode region (first 2 digits) if provided
        if (!string.IsNullOrWhiteSpace(postcode) && postcode.Length >= 2)
        {
            var postcodePrefix = postcode.Substring(0, 2);
            query = query.Where(q => q.Job.Postcode != null && q.Job.Postcode.StartsWith(postcodePrefix));
        }

        var prices = await query
            .Select(q => q.TotalCost)
            .ToListAsync(cancellationToken);

        // Require minimum sample size for valid benchmark
        if (prices.Count < MinimumSampleSize)
        {
            // Try without location filter
            if (!string.IsNullOrWhiteSpace(postcode))
            {
                return await GetBenchmarkAsync(tradeCategoryId, null, cancellationToken);
            }
            return null;
        }

        var sortedPrices = prices.OrderBy(p => p).ToList();

        return new PriceBenchmarkDto(
            tradeCategoryId,
            category.Name,
            postcode,
            MinPrice: sortedPrices.First(),
            MaxPrice: sortedPrices.Last(),
            AveragePrice: Math.Round(sortedPrices.Average(), 2),
            MedianPrice: CalculateMedian(sortedPrices),
            SampleSize: prices.Count,
            LastUpdated: DateTime.UtcNow
        );
    }

    public async Task<QuotePriceComparisonDto?> CompareQuotePriceAsync(Guid quoteId, CancellationToken cancellationToken = default)
    {
        var quote = await _context.Quotes
            .Include(q => q.Job)
            .ThenInclude(j => j.TradeCategory)
            .FirstOrDefaultAsync(q => q.Id == quoteId, cancellationToken);

        if (quote == null) return null;

        var benchmark = await GetBenchmarkAsync(
            quote.Job.TradeCategoryId,
            quote.Job.Postcode,
            cancellationToken);

        if (benchmark == null)
        {
            // Return comparison without benchmark data
            return new QuotePriceComparisonDto(
                quoteId,
                YourPrice: quote.TotalCost,
                MarketAverage: 0,
                PercentageDifference: 0,
                Rating: "Unknown",
                Explanation: "Not enough market data available for comparison",
                Benchmark: null
            );
        }

        var percentageDiff = CalculatePercentageDifference(quote.TotalCost, benchmark.AveragePrice);
        var rating = GetPriceRating(percentageDiff);
        var explanation = GetPriceExplanation(percentageDiff, benchmark.AveragePrice, quote.Job.TradeCategory.Name);

        return new QuotePriceComparisonDto(
            quoteId,
            YourPrice: quote.TotalCost,
            MarketAverage: benchmark.AveragePrice,
            PercentageDifference: percentageDiff,
            Rating: rating,
            Explanation: explanation,
            Benchmark: benchmark
        );
    }

    public async Task<TradieQuotesComparisonResponse> GetTradieQuotesComparisonAsync(Guid tradieId, CancellationToken cancellationToken = default)
    {
        var quotes = await _context.Quotes
            .Include(q => q.Job)
            .ThenInclude(j => j.TradeCategory)
            .Where(q => q.TradieId == tradieId)
            .Where(q => q.CreatedAt >= DateTime.UtcNow.AddMonths(-3)) // Last 3 months
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(cancellationToken);

        var comparisons = new List<QuotePriceComparisonDto>();
        var positionSum = 0m;
        var validComparisons = 0;
        var belowMarket = 0;
        var atMarket = 0;
        var aboveMarket = 0;

        foreach (var quote in quotes)
        {
            var comparison = await CompareQuotePriceAsync(quote.Id, cancellationToken);
            if (comparison != null)
            {
                comparisons.Add(comparison);

                if (comparison.Benchmark != null)
                {
                    positionSum += comparison.PercentageDifference;
                    validComparisons++;

                    if (comparison.PercentageDifference < -5)
                        belowMarket++;
                    else if (comparison.PercentageDifference > 5)
                        aboveMarket++;
                    else
                        atMarket++;
                }
            }
        }

        var averagePosition = validComparisons > 0 ? Math.Round(positionSum / validComparisons, 1) : 0;

        return new TradieQuotesComparisonResponse(
            comparisons,
            averagePosition,
            belowMarket,
            atMarket,
            aboveMarket
        );
    }

    public decimal CalculatePercentageDifference(decimal yourPrice, decimal marketAverage)
    {
        if (marketAverage == 0) return 0;
        return Math.Round(((yourPrice - marketAverage) / marketAverage) * 100, 1);
    }

    public string GetPriceRating(decimal percentageDifference)
    {
        return percentageDifference switch
        {
            > 20 => "Premium",
            > 5 => "Above Market",
            >= -5 => "At Market",
            >= -20 => "Below Market",
            _ => "Budget"
        };
    }

    public string GetPriceExplanation(decimal percentageDifference, decimal marketAverage, string tradeCategoryName)
    {
        var absPercentage = Math.Abs(percentageDifference);
        var direction = percentageDifference >= 0 ? "above" : "below";

        if (absPercentage <= 5)
        {
            return $"Your price is in line with market rates for {tradeCategoryName}s (avg ${marketAverage:N0})";
        }

        return percentageDifference switch
        {
            > 20 => $"Your price is {absPercentage:N0}% above market average. Consider if premium services justify this.",
            > 5 => $"Your price is {absPercentage:N0}% above market average (${marketAverage:N0}). Slightly higher than typical.",
            < -20 => $"Your price is {absPercentage:N0}% below market average. Very competitive, but ensure profitability.",
            < -5 => $"Your price is {absPercentage:N0}% below market average (${marketAverage:N0}). Competitive pricing.",
            _ => $"Your price is aligned with market rates for {tradeCategoryName}s"
        };
    }

    private static decimal CalculateMedian(List<decimal> sortedValues)
    {
        if (sortedValues.Count == 0) return 0;

        var mid = sortedValues.Count / 2;

        if (sortedValues.Count % 2 == 0)
        {
            return Math.Round((sortedValues[mid - 1] + sortedValues[mid]) / 2, 2);
        }

        return sortedValues[mid];
    }
}
