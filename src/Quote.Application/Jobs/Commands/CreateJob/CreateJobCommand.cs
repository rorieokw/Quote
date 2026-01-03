using MediatR;
using Quote.Application.Common.Models;
using Quote.Domain.Enums;

namespace Quote.Application.Jobs.Commands.CreateJob;

public record CreateJobCommand : IRequest<Result<CreateJobResponse>>
{
    public Guid TradeCategoryId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal? BudgetMin { get; init; }
    public decimal? BudgetMax { get; init; }
    public DateTime? PreferredStartDate { get; init; }
    public DateTime? PreferredEndDate { get; init; }
    public bool IsFlexibleDates { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string SuburbName { get; init; } = string.Empty;
    public AustralianState State { get; init; }
    public string Postcode { get; init; } = string.Empty;
    public PropertyType PropertyType { get; init; }
    public bool PublishImmediately { get; init; }
}

public record CreateJobResponse
{
    public Guid JobId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}
