using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Application.PriceBenchmarking.Services;
using Quote.Domain.Enums;
using Quote.Shared.DTOs;

namespace Quote.Application.Quotes.Queries.GetQuotesForComparison;

public class GetQuotesForComparisonQueryHandler : IRequestHandler<GetQuotesForComparisonQuery, Result<QuoteComparisonResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPriceBenchmarkingService _benchmarkingService;

    public GetQuotesForComparisonQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IPriceBenchmarkingService benchmarkingService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _benchmarkingService = benchmarkingService;
    }

    public async Task<Result<QuoteComparisonResponse>> Handle(GetQuotesForComparisonQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Result<QuoteComparisonResponse>.Failure("User not authenticated");
        }

        var job = await _context.Jobs
            .Include(j => j.TradeCategory)
            .Include(j => j.Quotes)
                .ThenInclude(q => q.Tradie)
                    .ThenInclude(t => t.TradieProfile)
                        .ThenInclude(tp => tp!.Licences)
            .FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken);

        if (job == null)
        {
            return Result<QuoteComparisonResponse>.Failure("Job not found");
        }

        if (job.CustomerId != _currentUserService.UserId.Value)
        {
            return Result<QuoteComparisonResponse>.Failure("You can only view quotes for your own jobs");
        }

        // Get market benchmark for price comparison
        var benchmark = await _benchmarkingService.GetBenchmarkAsync(
            job.TradeCategoryId,
            job.Postcode,
            cancellationToken);

        // Build quote comparison DTOs
        var quotes = job.Quotes
            .Where(q => q.Status == QuoteStatus.Pending || q.Status == QuoteStatus.Accepted)
            .Select(q =>
            {
                var profile = q.Tradie.TradieProfile;
                var isLicensed = profile?.Licences.Any(l => l.VerificationStatus == VerificationStatus.Verified) ?? false;
                var isInsured = profile?.InsuranceVerified ?? false;
                var isPoliceChecked = profile?.PoliceCheckVerified ?? false;
                var isIdentityVerified = profile?.IdentityVerified ?? false;

                var verificationLevel = GetVerificationLevel(isLicensed, isInsured, isPoliceChecked, isIdentityVerified);

                decimal? marketComparison = null;
                string priceRating = "Unknown";

                if (benchmark != null)
                {
                    marketComparison = _benchmarkingService.CalculatePercentageDifference(q.TotalCost, benchmark.AveragePrice);
                    priceRating = _benchmarkingService.GetPriceRating(marketComparison.Value);
                }

                return new QuoteComparisonDto(
                    QuoteId: q.Id,
                    TradieId: q.TradieId,
                    TradieName: $"{q.Tradie.FirstName} {q.Tradie.LastName}".Trim(),
                    TradieBusinessName: profile?.BusinessName,
                    TradiePhotoUrl: q.Tradie.ProfilePhotoUrl,
                    TradieRating: profile?.Rating ?? 0,
                    TradieReviewCount: profile?.TotalReviews ?? 0,
                    TradieJobsCompleted: profile?.TotalJobsCompleted ?? 0,
                    TradieCompletionRate: profile?.CompletionRate ?? 0,
                    TradieResponseTimeHours: profile?.AverageResponseTimeHours ?? 0,
                    Verification: new VerificationBadgesDto(
                        IsLicensed: isLicensed,
                        IsInsured: isInsured,
                        IsPoliceChecked: isPoliceChecked,
                        IsIdentityVerified: isIdentityVerified,
                        VerificationLevel: verificationLevel
                    ),
                    LabourCost: q.LabourCost,
                    MaterialsCost: q.MaterialsCost,
                    TotalCost: q.TotalCost,
                    EstimatedHours: q.EstimatedDurationHours,
                    ProposedStartDate: q.ProposedStartDate,
                    Notes: q.Notes,
                    DepositRequired: q.DepositRequired,
                    DepositAmount: q.RequiredDepositAmount ?? (q.RequiredDepositPercentage.HasValue ? q.TotalCost * q.RequiredDepositPercentage.Value / 100 : null),
                    CreatedAt: q.CreatedAt,
                    ValidUntil: q.ValidUntil,
                    PriceRating: priceRating,
                    MarketComparison: marketComparison
                );
            })
            .ToList();

        // Apply sorting
        quotes = request.SortBy?.ToLower() switch
        {
            "price" => request.SortDescending
                ? quotes.OrderByDescending(q => q.TotalCost).ToList()
                : quotes.OrderBy(q => q.TotalCost).ToList(),
            "rating" => request.SortDescending
                ? quotes.OrderByDescending(q => q.TradieRating).ToList()
                : quotes.OrderBy(q => q.TradieRating).ToList(),
            "experience" => request.SortDescending
                ? quotes.OrderByDescending(q => q.TradieJobsCompleted).ToList()
                : quotes.OrderBy(q => q.TradieJobsCompleted).ToList(),
            "response" => request.SortDescending
                ? quotes.OrderByDescending(q => q.TradieResponseTimeHours).ToList()
                : quotes.OrderBy(q => q.TradieResponseTimeHours).ToList(),
            "date" => request.SortDescending
                ? quotes.OrderByDescending(q => q.ProposedStartDate).ToList()
                : quotes.OrderBy(q => q.ProposedStartDate).ToList(),
            _ => quotes.OrderBy(q => q.TotalCost).ToList() // Default: lowest price first
        };

        var response = new QuoteComparisonResponse(
            JobId: job.Id,
            JobTitle: job.Title,
            TradeCategory: job.TradeCategory.Name,
            BudgetMin: job.BudgetMin,
            BudgetMax: job.BudgetMax,
            Quotes: quotes,
            TotalQuotes: quotes.Count,
            LowestPrice: quotes.Any() ? quotes.Min(q => q.TotalCost) : 0,
            HighestPrice: quotes.Any() ? quotes.Max(q => q.TotalCost) : 0,
            AveragePrice: quotes.Any() ? quotes.Average(q => q.TotalCost) : 0,
            MarketBenchmark: benchmark
        );

        return Result<QuoteComparisonResponse>.Success(response);
    }

    private static string GetVerificationLevel(bool isLicensed, bool isInsured, bool isPoliceChecked, bool isIdentityVerified)
    {
        var count = new[] { isLicensed, isInsured, isPoliceChecked, isIdentityVerified }.Count(x => x);

        return count switch
        {
            4 => "Premium",
            3 => "Verified",
            1 or 2 => "Basic",
            _ => "None"
        };
    }
}
