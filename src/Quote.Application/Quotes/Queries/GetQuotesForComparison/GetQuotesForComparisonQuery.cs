using MediatR;
using Quote.Application.Common.Models;
using Quote.Shared.DTOs;

namespace Quote.Application.Quotes.Queries.GetQuotesForComparison;

public record GetQuotesForComparisonQuery : IRequest<Result<QuoteComparisonResponse>>
{
    public Guid JobId { get; init; }
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; } = false;
}
