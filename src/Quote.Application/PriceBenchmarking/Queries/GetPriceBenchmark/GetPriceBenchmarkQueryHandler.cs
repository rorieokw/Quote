using MediatR;
using Quote.Application.Common.Models;
using Quote.Application.PriceBenchmarking.Services;
using Quote.Shared.DTOs;

namespace Quote.Application.PriceBenchmarking.Queries.GetPriceBenchmark;

public class GetPriceBenchmarkQueryHandler : IRequestHandler<GetPriceBenchmarkQuery, Result<PriceBenchmarkDto>>
{
    private readonly IPriceBenchmarkingService _benchmarkingService;

    public GetPriceBenchmarkQueryHandler(IPriceBenchmarkingService benchmarkingService)
    {
        _benchmarkingService = benchmarkingService;
    }

    public async Task<Result<PriceBenchmarkDto>> Handle(GetPriceBenchmarkQuery request, CancellationToken cancellationToken)
    {
        var benchmark = await _benchmarkingService.GetBenchmarkAsync(
            request.TradeCategoryId,
            request.Postcode,
            cancellationToken);

        if (benchmark == null)
        {
            return Result<PriceBenchmarkDto>.Failure("Not enough market data available for this trade category and location");
        }

        return Result<PriceBenchmarkDto>.Success(benchmark);
    }
}
