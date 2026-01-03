using MediatR;
using Quote.Application.Common.Models;
using Quote.Shared.DTOs;

namespace Quote.Application.PriceBenchmarking.Queries.CompareQuotePrice;

public record CompareQuotePriceQuery : IRequest<Result<QuotePriceComparisonDto>>
{
    public Guid QuoteId { get; init; }
}
