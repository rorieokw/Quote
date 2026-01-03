using MediatR;
using Quote.Application.Common.Models;
using Quote.Shared.DTOs;

namespace Quote.Application.PriceBenchmarking.Queries.GetTradieQuotesComparison;

public record GetTradieQuotesComparisonQuery : IRequest<Result<TradieQuotesComparisonResponse>>
{
}
