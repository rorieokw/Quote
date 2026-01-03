using MediatR;
using Quote.Application.Common.Models;
using Quote.Shared.DTOs;

namespace Quote.Application.MaterialBundles.Queries.GetBundleById;

public record GetBundleByIdQuery : IRequest<Result<MaterialBundleDto>>
{
    public Guid BundleId { get; init; }
}
