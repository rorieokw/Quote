using MediatR;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Application.PriceBenchmarking.Services;
using Quote.Shared.DTOs;

namespace Quote.Application.PriceBenchmarking.Queries.CompareQuotePrice;

public class CompareQuotePriceQueryHandler : IRequestHandler<CompareQuotePriceQuery, Result<QuotePriceComparisonDto>>
{
    private readonly IPriceBenchmarkingService _benchmarkingService;
    private readonly ICurrentUserService _currentUser;

    public CompareQuotePriceQueryHandler(
        IPriceBenchmarkingService benchmarkingService,
        ICurrentUserService currentUser)
    {
        _benchmarkingService = benchmarkingService;
        _currentUser = currentUser;
    }

    public async Task<Result<QuotePriceComparisonDto>> Handle(CompareQuotePriceQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return Result<QuotePriceComparisonDto>.Failure("User not authenticated");
        }

        var comparison = await _benchmarkingService.CompareQuotePriceAsync(request.QuoteId, cancellationToken);

        if (comparison == null)
        {
            return Result<QuotePriceComparisonDto>.Failure("Quote not found");
        }

        return Result<QuotePriceComparisonDto>.Success(comparison);
    }
}
