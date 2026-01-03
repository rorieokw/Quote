using MediatR;
using Quote.Application.Common.Models;

namespace Quote.Application.Jobs.Queries.GetJob;

public record GetJobQuery : IRequest<Result<JobDetailDto>>
{
    public Guid JobId { get; init; }
}

public record JobDetailDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public TradeCategoryDto TradeCategory { get; init; } = null!;
    public Guid TradeCategoryId { get; init; }
    public decimal? BudgetMin { get; init; }
    public decimal? BudgetMax { get; init; }
    public DateTime? PreferredStartDate { get; init; }
    public DateTime? PreferredEndDate { get; init; }
    public bool IsFlexibleDates { get; init; }
    public LocationDto Location { get; init; } = null!;
    public string PropertyType { get; init; } = string.Empty;
    public CustomerDto Customer { get; init; } = null!;
    public List<JobMediaDto> Media { get; init; } = new();
    public List<QuoteSummaryDto> Quotes { get; init; } = new();
    public DateTime CreatedAt { get; init; }
}

public record TradeCategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Icon { get; init; }
}

public record LocationDto
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string SuburbName { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Postcode { get; init; } = string.Empty;
}

public record CustomerDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string? ProfilePhotoUrl { get; init; }
}

public record JobMediaDto
{
    public Guid Id { get; init; }
    public string MediaUrl { get; init; } = string.Empty;
    public string MediaType { get; init; } = string.Empty;
    public string? Caption { get; init; }
    public string? ThumbnailUrl { get; init; }
}

public record QuoteSummaryDto
{
    public Guid Id { get; init; }
    public Guid TradieId { get; init; }
    public string TradieName { get; init; } = string.Empty;
    public decimal TotalCost { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
