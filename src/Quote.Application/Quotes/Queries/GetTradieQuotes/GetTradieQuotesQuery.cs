using MediatR;
using Quote.Application.Common.Models;

namespace Quote.Application.Quotes.Queries.GetTradieQuotes;

public record GetTradieQuotesQuery : IRequest<Result<GetTradieQuotesResponse>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Status { get; init; }  // Filter by status
}

public record GetTradieQuotesResponse
{
    public List<QuoteWithStatusDto> Quotes { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public record QuoteWithStatusDto
{
    public Guid QuoteId { get; init; }
    public Guid JobId { get; init; }
    public string JobTitle { get; init; } = string.Empty;
    public string JobSuburb { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal TotalCost { get; init; }
    public int ViewCount { get; init; }
    public DateTime? FirstViewedAt { get; init; }
    public DateTime? LastViewedAt { get; init; }
    public int CompetingQuotes { get; init; }
    public bool IsWinningQuote { get; init; }
    public bool IsJobStillOpen { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime ValidUntil { get; init; }
}
