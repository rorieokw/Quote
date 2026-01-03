using MediatR;
using Quote.Application.Common.Models;
using Quote.Shared.DTOs;

namespace Quote.Application.LeadScoring.Queries.GetScoredLeads;

public record GetScoredLeadsQuery : IRequest<Result<ScoredLeadsResponse>>
{
    public Guid TradieId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int? MinScore { get; init; }
    public string? ScoreRating { get; init; } // "Excellent", "Good", "Fair", "Low"
    public Guid? TradeCategoryId { get; init; }
    public bool RefreshScores { get; init; } = false;
}
