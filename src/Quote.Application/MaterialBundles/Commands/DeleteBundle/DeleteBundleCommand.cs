using MediatR;
using Quote.Application.Common.Models;

namespace Quote.Application.MaterialBundles.Commands.DeleteBundle;

public record DeleteBundleCommand : IRequest<Result<bool>>
{
    public Guid BundleId { get; init; }
}
