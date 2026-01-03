using MediatR;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Application.PriceBenchmarking.Services;
using Quote.Shared.DTOs;

namespace Quote.Application.PriceBenchmarking.Queries.GetTradieQuotesComparison;

public class GetTradieQuotesComparisonQueryHandler : IRequestHandler<GetTradieQuotesComparisonQuery, Result<TradieQuotesComparisonResponse>>
{
    private readonly IPriceBenchmarkingService _benchmarkingService;
    private readonly ICurrentUserService _currentUser;

    public GetTradieQuotesComparisonQueryHandler(
        IPriceBenchmarkingService benchmarkingService,
        ICurrentUserService currentUser)
    {
        _benchmarkingService = benchmarkingService;
        _currentUser = currentUser;
    }

    public async Task<Result<TradieQuotesComparisonResponse>> Handle(GetTradieQuotesComparisonQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return Result<TradieQuotesComparisonResponse>.Failure("User not authenticated");
        }

        var comparison = await _benchmarkingService.GetTradieQuotesComparisonAsync(
            _currentUser.UserId.Value,
            cancellationToken);

        return Result<TradieQuotesComparisonResponse>.Success(comparison);
    }
}
