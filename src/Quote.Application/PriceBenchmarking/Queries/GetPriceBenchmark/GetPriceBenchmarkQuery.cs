using MediatR;
using Quote.Application.Common.Models;
using Quote.Shared.DTOs;

namespace Quote.Application.PriceBenchmarking.Queries.GetPriceBenchmark;

public record GetPriceBenchmarkQuery : IRequest<Result<PriceBenchmarkDto>>
{
    public Guid TradeCategoryId { get; init; }
    public string? Postcode { get; init; }
}
