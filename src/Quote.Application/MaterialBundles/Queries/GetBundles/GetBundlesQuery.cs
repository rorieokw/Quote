using MediatR;
using Quote.Application.Common.Models;
using Quote.Shared.DTOs;

namespace Quote.Application.MaterialBundles.Queries.GetBundles;

public record GetBundlesQuery : IRequest<Result<MaterialBundleListResponse>>
{
    public Guid? TradeCategoryId { get; init; }
    public bool IncludeInactive { get; init; } = false;
}
