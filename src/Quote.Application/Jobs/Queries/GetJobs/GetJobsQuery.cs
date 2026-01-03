using MediatR;
using Quote.Application.Common.Models;
using Quote.Domain.Enums;

namespace Quote.Application.Jobs.Queries.GetJobs;

public record GetJobsQuery : IRequest<PaginatedList<JobDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public Guid? TradeCategoryId { get; init; }
    public AustralianState? State { get; init; }
    public string? Postcode { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public int? RadiusKm { get; init; }
    public decimal? MinBudget { get; init; }
    public decimal? MaxBudget { get; init; }
    public JobStatus? Status { get; init; }
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; } = true;
}

public record JobDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string TradeCategory { get; init; } = string.Empty;
    public Guid TradeCategoryId { get; init; }
    public decimal? BudgetMin { get; init; }
    public decimal? BudgetMax { get; init; }
    public DateTime? PreferredStartDate { get; init; }
    public string SuburbName { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Postcode { get; init; } = string.Empty;
    public double DistanceKm { get; init; }
    public int QuoteCount { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public List<string> MediaUrls { get; init; } = new();
}
